namespace TramWebApp.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.Persistence;

public class TramRunRepository : ITramRunRepository
{
    private readonly TramDbContext _context;

    public TramRunRepository(TramDbContext context)
    {
        _context = context;
    }

    public async Task<TramRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TramRuns
            .Include(x => x.TelemetryPoints)
            .Include(x => x.Inferences)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TramRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TramRuns
            .Include(x => x.TelemetryPoints)
            .Include(x => x.Inferences)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TramRun>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TramRuns
            .Include(x => x.TelemetryPoints)
            .Include(x => x.Inferences)
            .Where(x => x.CreatedByUserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TramRun> AddAsync(TramRun tramRun, CancellationToken cancellationToken = default)
    {
        _context.TramRuns.Add(tramRun);
        await _context.SaveChangesAsync(cancellationToken);
        return tramRun;
    }

    public async Task<TramRun> UpdateAsync(TramRun tramRun, CancellationToken cancellationToken = default)
    {
        _context.TramRuns.Update(tramRun);
        await _context.SaveChangesAsync(cancellationToken);
        return tramRun;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TramRuns.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.TramRuns.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TramRuns.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TramRuns.AnyAsync(x => x.Id == id, cancellationToken);
    }
}