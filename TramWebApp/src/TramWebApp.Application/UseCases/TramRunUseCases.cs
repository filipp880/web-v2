namespace TramWebApp.Application.UseCases;

using TramWebApp.Application.Common;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
using AutoMapper;

public class TramRunUseCases : ITramRunUseCases
{
    private readonly ITramRunRepository _repository;
    private readonly IMapper _mapper;

    public TramRunUseCases(ITramRunRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<TramRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity != null ? _mapper.Map<TramRunDto>(entity) : null;
    }

    public async Task<IReadOnlyList<TramRunListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<TramRunListDto>>(entities);
    }

    public async Task<IReadOnlyList<TramRunListDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<IReadOnlyList<TramRunListDto>>(entities);
    }

    public async Task<TramRunDto> CreateAsync(CreateTramRunDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TramRun>(dto);
        entity = new TramRun(dto.Name, dto.Description, userId);
        
        var created = await _repository.AddAsync(entity, cancellationToken);
        return _mapper.Map<TramRunDto>(created);
    }

    public async Task<TramRunDto> UpdateAsync(Guid id, UpdateTramRunDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("TramRun", id);

        if (entity.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Нет прав на изменение этого прогона");

        entity.Update(dto.Name, dto.Description);
        var updated = await _repository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<TramRunDto>(updated);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            throw new EntityNotFoundException("TramRun", id);

        if (entity.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Нет прав на удаление этого прогона");

        await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetCountAsync(cancellationToken);
    }
}