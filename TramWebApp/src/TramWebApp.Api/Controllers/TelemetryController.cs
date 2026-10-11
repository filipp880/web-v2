namespace TramWebApp.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;

[ApiController]
[Route("api/tram-runs/{tramRunId:guid}/telemetry")]
[Authorize]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryUseCases _useCases;

    public TelemetryController(ITelemetryUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TramTelemetryPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByRunId(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        var points = await _useCases.GetByRunIdAsync(tramRunId, cancellationToken);
        return Ok(points);
    }

    [HttpGet("{sequenceNumber:int}")]
    [ProducesResponseType(typeof(TramTelemetryPointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySequence(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default)
    {
        var point = await _useCases.GetByRunIdAndSequenceAsync(tramRunId, sequenceNumber, cancellationToken);
        if (point == null)
            return NotFound($"Телеметрия с номером {sequenceNumber} не найдена");
        return Ok(point);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TramTelemetryPointDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid tramRunId, [FromBody] CreateTelemetryPointDto dto, CancellationToken cancellationToken = default)
    {
        var point = await _useCases.AddAsync(tramRunId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetBySequence), new { tramRunId, sequenceNumber = point.SequenceNumber }, point);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<TramTelemetryPointDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBatch(Guid tramRunId, [FromBody] IEnumerable<CreateTelemetryPointDto> dtos, CancellationToken cancellationToken = default)
    {
        var points = await _useCases.AddRangeAsync(tramRunId, dtos, cancellationToken);
        return CreatedAtAction(nameof(GetByRunId), new { tramRunId }, points);
    }
}