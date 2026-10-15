namespace TramWebApp.Application.Interfaces;
using TramWebApp.Application.DTOs;
public interface ISimulationUseCases
{
    Task<SimulationCreatedDto> StartAsync(Guid sourceId, SimulationStartDto request, CancellationToken cancellationToken = default);
    Task<SimulationStateDto> StepAsync(Guid id, SimulationStepDto request, CancellationToken cancellationToken = default);
}
