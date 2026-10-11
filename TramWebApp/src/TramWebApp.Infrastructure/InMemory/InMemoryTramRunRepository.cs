namespace TramWebApp.Infrastructure.InMemory;

using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
using TramWebApp.Domain.Enums;

public class InMemoryTramRunRepository : ITramRunRepository
{
    private readonly ConcurrentDictionary<Guid, TramRun> _runs = new();
    private readonly int _maxRecords;
    private readonly object _lock = new();

    public InMemoryTramRunRepository(int maxRecords = 50)
    {
        _maxRecords = maxRecords;
    }

    public Task<TramRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _runs.TryGetValue(id, out var run);
        return Task.FromResult(run);
    }

    public Task<IReadOnlyList<TramRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TramRun>>(_runs.Values.OrderByDescending(x => x.CreatedAt).ToList());
    }

    public Task<IReadOnlyList<TramRun>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TramRun>>(_runs.Values.Where(x => x.CreatedByUserId == userId).OrderByDescending(x => x.CreatedAt).ToList());
    }

    public Task<TramRun> AddAsync(TramRun tramRun, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_runs.Count >= _maxRecords)
                throw new InMemoryStorageLimitException(_maxRecords);

            _runs.TryAdd(tramRun.Id, tramRun);
        }
        return Task.FromResult(tramRun);
    }

    public Task<TramRun> UpdateAsync(TramRun tramRun, CancellationToken cancellationToken = default)
    {
        _runs.AddOrUpdate(tramRun.Id, tramRun, (_, _) => tramRun);
        return Task.FromResult(tramRun);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _runs.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_runs.Count);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_runs.ContainsKey(id));
    }
}