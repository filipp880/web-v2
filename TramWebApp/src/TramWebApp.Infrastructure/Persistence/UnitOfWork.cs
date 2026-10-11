namespace TramWebApp.Infrastructure.Persistence;

using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly TramDbContext _context;
    private TramRunRepository? _tramRuns;
    private TramTelemetryRepository? _telemetryPoints;
    private ModelInferenceRepository? _inferences;
    private UserRepository? _users;

    public UnitOfWork(TramDbContext context)
    {
        _context = context;
    }

    public ITramRunRepository TramRuns => _tramRuns ??= new TramRunRepository(_context);
    public ITramTelemetryRepository TelemetryPoints => _telemetryPoints ??= new TramTelemetryRepository(_context);
    public IModelInferenceRepository Inferences => _inferences ??= new ModelInferenceRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}