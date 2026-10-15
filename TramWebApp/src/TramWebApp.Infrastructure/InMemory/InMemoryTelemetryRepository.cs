namespace TramWebApp.Infrastructure.InMemory;
using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
public class InMemoryTelemetryRepository(ITramRunRepository runs) : ITramTelemetryRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, TramTelemetryPoint>> _points = new();
    private readonly object _gate = new();
    public Task<IReadOnlyList<TramTelemetryPoint>> GetByRunIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TramTelemetryPoint>>(_points.TryGetValue(id, out var points) ? points.Values.OrderBy(point => point.SequenceNumber).ToArray() : []);
    public Task<TramTelemetryPoint?> GetByRunIdAndSequenceAsync(Guid id, int sequence, CancellationToken cancellationToken = default) =>
        Task.FromResult(_points.TryGetValue(id, out var points) && points.TryGetValue(sequence, out var point) ? point : null);
    public async Task<TramTelemetryPoint> AddAsync(TramTelemetryPoint point, CancellationToken cancellationToken = default)
    {
        await AddRangeAsync([point], cancellationToken); return point;
    }
    public async Task<IReadOnlyList<TramTelemetryPoint>> AddRangeAsync(IEnumerable<TramTelemetryPoint> points, CancellationToken cancellationToken = default)
    {
        var list = points.ToList();
        var owners = new Dictionary<Guid, TramRun>();
        foreach (var id in list.Select(point => point.TramRunId).Distinct())
            owners[id] = await runs.GetByIdAsync(id, cancellationToken) ?? throw new EntityNotFoundException("TramRun", id);
        lock (_gate)
        {
            if (list.GroupBy(point => (point.TramRunId, point.SequenceNumber)).Any(group => group.Count() > 1) ||
                list.Any(point => _points.TryGetValue(point.TramRunId, out var stored) && stored.ContainsKey(point.SequenceNumber)))
                throw new ConflictException("Точка с таким номером уже существует.");
            foreach (var point in list)
            {
                _points.GetOrAdd(point.TramRunId, _ => new())[point.SequenceNumber] = point;
                owners[point.TramRunId].TelemetryPoints.Add(point);
            }
        }
        return list;
    }
    public Task<int> GetCountByRunIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_points.TryGetValue(id, out var points) ? points.Count : 0);
    public async Task DeleteByRunAsync(Guid id)
    {
        var run = await runs.GetByIdAsync(id);
        lock (_gate) { _points.TryRemove(id, out _); run?.TelemetryPoints.Clear(); }
    }
}
