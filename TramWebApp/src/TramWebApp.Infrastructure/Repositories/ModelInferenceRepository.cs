namespace TramWebApp.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.Persistence;

public class ModelInferenceRepository : IModelInferenceRepository
{
    private readonly TramDbContext _context;

    public ModelInferenceRepository(TramDbContext context)
    {
        _context = context;
    }

    public async Task<ModelInference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Inferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ModelInference>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        return await _context.Inferences
            .Where(x => x.TramRunId == tramRunId)
            .OrderBy(x => x.InferenceTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModelInference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Inferences
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.InferenceTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ModelInference> AddAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        _context.Inferences.Add(inference);
        await _context.SaveChangesAsync(cancellationToken);
        return inference;
    }

    public async Task<ModelInference> UpdateAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        _context.Inferences.Update(inference);
        await _context.SaveChangesAsync(cancellationToken);
        return inference;
    }

    public async Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        return await _context.Inferences.CountAsync(x => x.TramRunId == tramRunId, cancellationToken);
    }
}