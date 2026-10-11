namespace TramWebApp.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.Persistence;

public class TramTelemetryRepository : ITramTelemetryRepository
{
    private readonly TramDbContext _context;

    public TramTelemetryRepository(TramDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TramTelemetryPoint>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        return await _context.TelemetryPoints
            .Where(x => x.TramRunId == tramRunId)
            .OrderBy(x => x.SequenceNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TramTelemetryPoint?> GetByRunIdAndSequenceAsync(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.TelemetryPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TramRunId == tramRunId && x.SequenceNumber == sequenceNumber, cancellationToken);
    }

    public async Task<TramTelemetryPoint> AddAsync(TramTelemetryPoint point, CancellationToken cancellationToken = default)
    {
        _context.TelemetryPoints.Add(point);
        await _context.SaveChangesAsync(cancellationToken);
        return point;
    }

    public async Task<IReadOnlyList<TramTelemetryPoint>> AddRangeAsync(IEnumerable<TramTelemetryPoint> points, CancellationToken cancellationToken = default)
    {
        var list = points.ToList();
        _context.TelemetryPoints.AddRange(list);
        await _context.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        return await _context.TelemetryPoints.CountAsync(x => x.TramRunId == tramRunId, cancellationToken);
    }
}