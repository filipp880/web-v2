namespace TramWebApp.Application.UseCases;

using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
using AutoMapper;

public class TelemetryUseCases : ITelemetryUseCases
{
    private readonly ITramTelemetryRepository _repository;
    private readonly IRunAccess _access;
    private readonly IMapper _mapper;

    public TelemetryUseCases(
        ITramTelemetryRepository repository,
        IRunAccess access,
        IMapper mapper)
    {
        _repository = repository;
        _access = access;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TramTelemetryPointDto>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        await _access.RequireAsync(tramRunId, cancellationToken);

        var entities = await _repository.GetByRunIdAsync(tramRunId, cancellationToken);
        return _mapper.Map<IReadOnlyList<TramTelemetryPointDto>>(entities);
    }

    public async Task<TramTelemetryPointDto?> GetByRunIdAndSequenceAsync(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default)
    {
        await _access.RequireAsync(tramRunId, cancellationToken);

        var entity = await _repository.GetByRunIdAndSequenceAsync(tramRunId, sequenceNumber, cancellationToken);
        return entity != null ? _mapper.Map<TramTelemetryPointDto>(entity) : null;
    }

    public async Task<TramTelemetryPointDto> AddAsync(Guid tramRunId, CreateTelemetryPointDto dto, CancellationToken cancellationToken = default)
    {
        await _access.RequireAsync(tramRunId, cancellationToken);

        var entity = _mapper.Map<TramTelemetryPoint>(dto, opt => opt.Items["TramRunId"] = tramRunId);
        var created = await _repository.AddAsync(entity, cancellationToken);
        return _mapper.Map<TramTelemetryPointDto>(created);
    }

    public async Task<IReadOnlyList<TramTelemetryPointDto>> AddRangeAsync(Guid tramRunId, IEnumerable<CreateTelemetryPointDto> dtos, CancellationToken cancellationToken = default)
    {
        await _access.RequireAsync(tramRunId, cancellationToken);

        var entities = dtos.Select(dto => _mapper.Map<TramTelemetryPoint>(dto, opt => opt.Items["TramRunId"] = tramRunId)).ToList();
        var created = await _repository.AddRangeAsync(entities, cancellationToken);
        return _mapper.Map<IReadOnlyList<TramTelemetryPointDto>>(created);
    }
}