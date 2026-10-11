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
    private readonly ITramRunRepository _runRepository;
    private readonly IMapper _mapper;

    public ModelUseCases(
        IModelService modelService,
        IModelInferenceRepository inferenceRepository,
        ITramRunRepository runRepository,
        IMapper mapper)
    {
        _modelService = modelService;
        _inferenceRepository = inferenceRepository;
        _runRepository = runRepository;
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
        var runExists = await _runRepository.ExistsAsync(request.TramRunId, cancellationToken);
        if (!runExists)
            throw new EntityNotFoundException("TramRun", request.TramRunId);

        var modelInput = _mapper.Map<ModelInput>(request.Input);
        var modelOutput = await _modelService.PredictAsync(modelInput, cancellationToken);

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
        var runExists = await _runRepository.ExistsAsync(tramRunId, cancellationToken);
        if (!runExists)
            throw new EntityNotFoundException("TramRun", tramRunId);

        var entities = await _inferenceRepository.GetByRunIdAsync(tramRunId, cancellationToken);
        return _mapper.Map<IReadOnlyList<ModelInferenceListDto>>(entities);
    }

    public async Task<IReadOnlyList<ModelInferenceListDto>> GetInferencesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await _inferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<IReadOnlyList<ModelInferenceListDto>>(entities);
    }

    public async Task<ModelInferenceDto?> GetInferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(id, cancellationToken);
        return entity != null ? _mapper.Map<ModelInferenceDto>(entity) : null;
    }

    public async Task<ModelInferenceDto> UpdateInferenceWithUserInterventionAsync(Guid inferenceId, double adjustedSpeed, double adjustedPosition, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(inferenceId, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("ModelInference", inferenceId);

        entity.SetUserIntervention(adjustedSpeed, adjustedPosition);
        var updated = await _inferenceRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<ModelInferenceDto>(updated);
    }

    public async Task<ModelInferenceDto> UpdateInferenceWithActualValuesAsync(Guid inferenceId, double actualSpeed, double actualPosition, CancellationToken cancellationToken = default)
    {
        var entity = await _inferenceRepository.GetByIdAsync(inferenceId, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("ModelInference", inferenceId);

        entity.SetActualValues(actualSpeed, actualPosition);
        var updated = await _inferenceRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<ModelInferenceDto>(updated);
    }

    public bool IsModelLoaded => _modelService.IsModelLoaded;
    public string ModelVersion => _modelService.ModelVersion;
}