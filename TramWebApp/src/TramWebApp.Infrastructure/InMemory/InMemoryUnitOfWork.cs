namespace TramWebApp.Infrastructure.InMemory;

using TramWebApp.Domain.Interfaces;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public ITramRunRepository TramRuns { get; }
    public ITramTelemetryRepository TelemetryPoints { get; }
    public IModelInferenceRepository Inferences { get; }
    public IUserRepository Users { get; }

    public InMemoryUnitOfWork(int maxRecords = 50)
    {
        var runs = new InMemoryTramRunRepository(maxRecords);
        var points = new InMemoryTelemetryRepository(runs);
        var inferences = new InMemoryModelInferenceRepository(runs);
        runs.DeleteChildren = async id => { await points.DeleteByRunAsync(id); await inferences.DeleteByRunAsync(id); };
        TramRuns = runs; TelemetryPoints = points; Inferences = inferences;
        Users = new InMemoryUserRepository();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public void Dispose()
    {
    }
}
