namespace TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Entities;
public interface ISimulationRepository
{
    Task<TramTelemetryPoint?> GetLastPointAsync(Guid runId, CancellationToken cancellationToken = default);
    Task CreateAsync(TramRun run, TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default);
    Task AppendAsync(TramTelemetryPoint point, ModelInference inference, CancellationToken cancellationToken = default);
}
