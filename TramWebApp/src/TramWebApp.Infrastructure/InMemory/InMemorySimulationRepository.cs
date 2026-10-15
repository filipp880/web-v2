namespace TramWebApp.Infrastructure.InMemory;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
public class InMemorySimulationRepository(IUnitOfWork storage) : ISimulationRepository
{
    public async Task<TramTelemetryPoint?> GetLastPointAsync(Guid runId, CancellationToken cancellationToken = default) =>
        (await storage.TelemetryPoints.GetByRunIdAsync(runId, cancellationToken)).LastOrDefault();
    public async Task CreateAsync(TramRun run, TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default)
    {
        await storage.TramRuns.AddAsync(run, cancellationToken);
        await AppendAsync(point, inference, cancellationToken);
    }
    public async Task AppendAsync(TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default)
    {
        await storage.TelemetryPoints.AddAsync(point, cancellationToken);
        await storage.Inferences.AddAsync(inference, cancellationToken);
    }
}
