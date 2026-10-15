namespace TramWebApp.Application.UseCases;

using TramWebApp.Application.Common;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.ValueObjects;
using TramWebApp.Domain.Exceptions;
using AutoMapper;

public class ModelUseCases : IModelUseCases
{
    private readonly IModelService _modelService;
    private readonly IModelInferenceRepository _inferenceRepository;
    private readonly IRunAccess _access;
    private readonly IMapper _mapper;

    public ModelUseCases(
        IModelService modelService,
        IModelInferenceRepository inferenceRepository,
        IRunAccess access,
        IMapper mapper)
    {
        _modelService = modelService;
        _inferenceRepository = inferenceRepository;
        _access = access;
        _mapper = mapper;
    }

    public Task<ModelOutputDto> PredictAsync(ModelInputDto input, CancellationToken cancellationToken = default)
    {
        var modelInput = _mapper.Map<ModelInput>(input);
        var output = _modelService.PredictAsync(modelInput, cancellationToken);
        return output.ContinueWith(t => _mapper.Map<ModelOutputDto>(t.Result), cancellationToken);
    }

    public Task<IReadOnlyList<ModelOutputDto>> PredictBatchAsync(IEnumerable<ModelInputDto> inputs, CancellationToken cancellationToken = default)
    {
        var modelInputs = _mapper.Map<IEnumerable<ModelInput>>(inputs);
        var outputs = _modelService.PredictBatchAsync(modelInputs, cancellationToken);
        return outputs.ContinueWith(t => _mapper.Map<IReadOnlyList<ModelOutputDto>>(t.Result), cancellationToken);
    }

    public Task<MetricsSnapshotDto> CalculateMetricsAsync(ModelInputDto input, ModelOutputDto output, double? actualSpeed = null, double? actualPosition = null, CancellationToken cancellationToken = default)
    {
        var modelInput = _mapper.Map<ModelInput>(input);
        var modelOutput = _mapper.Map<ModelOutput>(output);
        var metrics = _modelService.CalculateMetricsAsync(modelInput, modelOutput, actualSpeed, actualPosition, cancellationToken);
        return metrics.ContinueWith(t => _mapper.Map<MetricsSnapshotDto>(t.Result), cancellationToken);
    }

    public async Task<ModelInferenceDto> RunInferenceAsync(InferenceRequestDto request, Guid? userId, CancellationToken cancellationToken = default)
    {
        var run = await _access.RequireAsync(request.TramRunId, cancellationToken);

        var modelInput = _mapper.Map<ModelInput>(request.Input);
        var modelOutput = await _modelService.PredictAsync(modelInput, cancellationToken);
        var precedingPoints = run!.TelemetryPoints.Where(point => point.SequenceNumber <= request.SequenceNumber)
            .OrderBy(point => point.SequenceNumber).ToArray();
        if (precedingPoints.Length > 0)
        {
            var inputs = precedingPoints.Select(point => new ModelInput(
                point.ControllerHandlePosition, point.FrontBogieSpeed, point.RearBogieSpeed, point.Timestamp,
                point.GpsLatitude, point.GpsLongitude, point.GpsSpeed)).ToArray();
            // Recalculate the submitted scenario on the server; never trust a client distance.
            if (request.ScenarioInputs is { } scenario)
            {
                if (scenario.Count != precedingPoints.Length || scenario.Count > 20000)
                    throw new InvalidModelInputException("Сценарий должен содержать все шаги до выбранной точки.");
                for (var index = 0; index < scenario.Count; index++)
                {
                    var signal = scenario[index];
                    if (signal.Timestamp != precedingPoints[index].Timestamp ||
                        !double.IsFinite(signal.ControllerHandlePosition) || Math.Abs(signal.ControllerHandlePosition) > 1 ||
                        !double.IsFinite(signal.FrontBogieSpeed) || signal.FrontBogieSpeed < 0 || signal.FrontBogieSpeed > 100 ||
                        !double.IsFinite(signal.RearBogieSpeed) || signal.RearBogieSpeed < 0 || signal.RearBogieSpeed > 100 ||
                        signal.GpsSpeed is double gps && (!double.IsFinite(gps) || gps < 0 || gps > 100))
                        throw new InvalidModelInputException("Некорректные значения сигналов сценария.");
                }
                if (scenario[^1] != request.Input)
                    throw new InvalidModelInputException("Последняя точка сценария должна совпадать с входом модели.");
                inputs = _mapper.Map<ModelInput[]>(scenario);
            }
            else inputs[^1] = modelInput;
            var batch = await _modelService.PredictBatchAsync(inputs, cancellationToken);
            modelOutput = new ModelOutput(modelOutput.PredictedSpeed, batch[^1].PredictedPosition, modelOutput.Timestamp, modelOutput.Confidence);
        }

        var inference = new ModelInference(
            request.TramRunId,
            userId,
            request.Input.ControllerHandlePosition,
            request.Input.FrontBogieSpeed,
            request.Input.RearBogieSpeed,
            modelOutput.PredictedSpeed,
            modelOutput.PredictedPosition,
            isUserIntervention: false);

        var created = await _inferenceRepository.AddAsync(inference, cancellationToken);
        return _mapper.Map<ModelInferenceDto>(created);
    }

    public async Task<IReadOnlyList<ModelInferenceListDto>> GetInferencesByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        await _access.RequireAsync(tramRunId, cancellationToken);

        var entities = await _inferenceRepository.GetByRunIdAsync(tramRunId, cancellationToken);
        return _mapper.Map<IReadOnlyList<ModelInferenceListDto>>(entities);
    }

    public async Task<IReadOnlyList<ModelInferenceListDto>> GetInferencesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await _inferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        var allowedRuns = new HashSet<Guid>();
        foreach (var runId in entities.Select(entity => entity.TramRunId).Distinct())
        {
            try { await _access.RequireAsync(runId, cancellationToken); allowedRuns.Add(runId); }
            catch (UnauthorizedAccessException) { }
            catch (EntityNotFoundException) { }
        }
        return _mapper.Map<IReadOnlyList<ModelInferenceListDto>>(entities.Where(entity => allowedRuns.Contains(entity.TramRunId)));
    }

    public async Task<ModelInferenceDto?> GetInferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(id, cancellationToken);
        if (entity != null) await _access.RequireAsync(entity.TramRunId, cancellationToken);
        return entity != null ? _mapper.Map<ModelInferenceDto>(entity) : null;
    }

    public async Task<ModelInferenceDto> UpdateInferenceWithUserInterventionAsync(Guid inferenceId, double adjustedSpeed, double adjustedPosition, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(inferenceId, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("ModelInference", inferenceId);

        await _access.RequireAsync(entity.TramRunId, cancellationToken);
        entity.SetUserIntervention(adjustedSpeed, adjustedPosition);
        var updated = await _inferenceRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<ModelInferenceDto>(updated);
    }

    public async Task<ModelInferenceDto> UpdateInferenceWithActualValuesAsync(Guid inferenceId, double actualSpeed, double actualPosition, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(inferenceId, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("ModelInference", inferenceId);

        await _access.RequireAsync(entity.TramRunId, cancellationToken);
        entity.SetActualValues(actualSpeed, actualPosition);
        var updated = await _inferenceRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<ModelInferenceDto>(updated);
    }

    public bool IsModelLoaded => _modelService.IsModelLoaded;
    public string ModelVersion => _modelService.ModelVersion;
}
