namespace TramWebApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.Persistence;
public class SimulationRepository(TramDbContext db) : ISimulationRepository
{
    public Task<TramTelemetryPoint?> GetLastPointAsync(Guid runId, CancellationToken cancellationToken = default) =>
        db.TelemetryPoints.AsNoTracking().Where(point => point.TramRunId == runId)
            .OrderByDescending(point => point.SequenceNumber).FirstOrDefaultAsync(cancellationToken);
    public async Task CreateAsync(TramRun run, TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default)
    {
        db.TramRuns.Add(run);
        await AppendAsync(point, inference, cancellationToken);
    }
    public async Task AppendAsync(TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default)
    {
        db.TelemetryPoints.Add(point); db.Inferences.Add(inference);
        await db.SaveChangesAsync(cancellationToken);
    }
}
