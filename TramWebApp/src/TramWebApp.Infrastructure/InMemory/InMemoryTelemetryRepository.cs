namespace TramWebApp.Infrastructure.InMemory;

using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;

public class InMemoryTelemetryRepository : ITramTelemetryRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, TramTelemetryPoint>> _points = new();

    public Task<IReadOnlyList<TramTelemetryPoint>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        if (_points.TryGetValue(tramRunId, out var runPoints))
        {
            return Task.FromResult<IReadOnlyList<TramTelemetryPoint>>(runPoints.Values.OrderBy(x => x.SequenceNumber).ToList());
        }
        return Task.FromResult<IReadOnlyList<TramTelemetryPoint>>(Array.Empty<TramTelemetryPoint>());
    }

    public Task<TramTelemetryPoint?> GetByRunIdAndSequenceAsync(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default)
    {
        if (_points.TryGetValue(tramRunId, out var runPoints))
        {
            runPoints.TryGetValue(sequenceNumber, out var point);
            return Task.FromResult(point);
        }
        return Task.FromResult<TramTelemetryPoint?>(null);
    }

    public Task<TramTelemetryPoint> AddAsync(TramTelemetryPoint point, CancellationToken cancellationToken = default)
    {
        var runPoints = _points.GetOrAdd(point.TramRunId, _ => new ConcurrentDictionary<int, TramTelemetryPoint>());
        runPoints.TryAdd(point.SequenceNumber, point);
        return Task.FromResult(point);
    }

    public Task<IReadOnlyList<TramTelemetryPoint>> AddRangeAsync(IEnumerable<TramTelemetryPoint> points, CancellationToken cancellationToken = default)
    {
        var list = points.ToList();
        foreach (var point in list)
        {
            var runPoints = _points.GetOrAdd(point.TramRunId, _ => new ConcurrentDictionary<int, TramTelemetryPoint>());
            runPoints.TryAdd(point.SequenceNumber, point);
        }
        return Task.FromResult<IReadOnlyList<TramTelemetryPoint>>(list);
    }

    public Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        if (_points.TryGetValue(tramRunId, out var runPoints))
        {
            return Task.FromResult(runPoints.Count);
        }
        return Task.FromResult(0);
    }
}