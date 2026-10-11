namespace TramWebApp.Infrastructure.InMemory;

using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;

public class InMemoryModelInferenceRepository : IModelInferenceRepository
{
    private readonly ConcurrentDictionary<Guid, ModelInference> _inferences = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _byRunId = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _byUserId = new();

    public Task<ModelInference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _inferences.TryGetValue(id, out var inference);
        return Task.FromResult(inference);
    }

    public Task<IReadOnlyList<ModelInference>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        if (_byRunId.TryGetValue(tramRunId, out var ids))
        {
            var result = ids.Select(id => _inferences.TryGetValue(id, out var inf) ? inf : null)
                           .Where(x => x != null)
                           .Cast<ModelInference>()
                           .OrderBy(x => x.InferenceTime)
                           .ToList();
            return Task.FromResult<IReadOnlyList<ModelInference>>(result);
        }
        return Task.FromResult<IReadOnlyList<ModelInference>>(Array.Empty<ModelInference>());
    }

    public Task<IReadOnlyList<ModelInference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_byUserId.TryGetValue(userId, out var ids))
        {
            var result = ids.Select(id => _inferences.TryGetValue(id, out var inf) ? inf : null)
                           .Where(x => x != null)
                           .Cast<ModelInference>()
                           .OrderByDescending(x => x.InferenceTime)
                           .ToList();
            return Task.FromResult<IReadOnlyList<ModelInference>>(result);
        }
        return Task.FromResult<IReadOnlyList<ModelInference>>(Array.Empty<ModelInference>());
    }

    public Task<ModelInference> AddAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        _inferences.TryAdd(inference.Id, inference);
        
        _byRunId.AddOrUpdate(inference.TramRunId,
            _ => new List<Guid> { inference.Id },
            (_, list) => { list.Add(inference.Id); return list; });

        if (inference.UserId.HasValue)
        {
            _byUserId.AddOrUpdate(inference.UserId.Value,
                _ => new List<Guid> { inference.Id },
                (_, list) => { list.Add(inference.Id); return list; });
        }

        return Task.FromResult(inference);
    }

    public Task<ModelInference> UpdateAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        _inferences.AddOrUpdate(inference.Id, inference, (_, _) => inference);
        return Task.FromResult(inference);
    }

    public Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        if (_byRunId.TryGetValue(tramRunId, out var ids))
        {
            return Task.FromResult(ids.Count);
        }
        return Task.FromResult(0);
    }
}