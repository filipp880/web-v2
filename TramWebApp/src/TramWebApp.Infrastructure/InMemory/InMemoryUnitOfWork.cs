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
        TramRuns = new InMemoryTramRunRepository(maxRecords);
        TelemetryPoints = new InMemoryTelemetryRepository();
        Inferences = new InMemoryModelInferenceRepository();
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