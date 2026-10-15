namespace TramWebApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;

[ApiController, Authorize, Route("api/simulation")]
public class SimulationController(ISimulationUseCases useCases) : ControllerBase
{
    [HttpPost("{sourceId:guid}/start")]
    [ProducesResponseType(typeof(SimulationCreatedDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(Guid sourceId, SimulationStartDto request, CancellationToken cancellationToken)
    {
        var result = await useCases.StartAsync(sourceId, request, cancellationToken);
        return CreatedAtAction(nameof(TramRunsController.GetById), "TramRuns", new { id = result.RunId }, result);
    }

    [HttpPost("{id:guid}/step")]
    [ProducesResponseType(typeof(SimulationStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Step(Guid id, SimulationStepDto request, CancellationToken cancellationToken) =>
        Ok(await useCases.StepAsync(id, request, cancellationToken));
}
