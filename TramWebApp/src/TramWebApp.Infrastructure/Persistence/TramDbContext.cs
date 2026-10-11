namespace TramWebApp.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Enums;

public class TramDbContext : DbContext
{
    public DbSet<TramRun> TramRuns => Set<TramRun>();
    public DbSet<TramTelemetryPoint> TelemetryPoints => Set<TramTelemetryPoint>();
    public DbSet<ModelInference> Inferences => Set<ModelInference>();
    public DbSet<User> Users => Set<User>();

    public TramDbContext(DbContextOptions<TramDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TramDbContext).Assembly);
    }
}