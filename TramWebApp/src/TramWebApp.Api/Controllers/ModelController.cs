namespace TramWebApp.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Application.Common;

[ApiController]
[Route("api/model")]
[Authorize]
public class ModelController : ControllerBase
{
    private readonly IModelUseCases _useCases;

    public ModelController(IModelUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpPost("predict")]
    [ProducesResponseType(typeof(ModelOutputDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Predict([FromBody] ModelInputDto input, CancellationToken cancellationToken = default)
    {
        var output = await _useCases.PredictAsync(input, cancellationToken);
        return Ok(output);
    }

    [HttpPost("predict/batch")]
    [ProducesResponseType(typeof(IReadOnlyList<ModelOutputDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PredictBatch([FromBody] IEnumerable<ModelInputDto> inputs, CancellationToken cancellationToken = default)
    {
        var outputs = await _useCases.PredictBatchAsync(inputs, cancellationToken);
        return Ok(outputs);
    }

    [HttpPost("metrics")]
    [ProducesResponseType(typeof(MetricsSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateMetrics([FromBody] MetricsCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var metrics = await _useCases.CalculateMetricsAsync(request.Input, request.Output, request.ActualSpeed, request.ActualPosition, cancellationToken);
        return Ok(metrics);
    }

    [HttpPost("inference")]
    [ProducesResponseType(typeof(ModelInferenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunInference([FromBody] InferenceRequestDto request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var inference = await _useCases.RunInferenceAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetInference), new { id = inference.Id }, inference);
    }

    [HttpGet("inferences/run/{tramRunId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ModelInferenceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInferencesByRun(Guid tramRunId, CancellationToken cancellationToken = default)
    {
        var inferences = await _useCases.GetInferencesByRunIdAsync(tramRunId, cancellationToken);
        return Ok(inferences);
    }

    [HttpGet("inferences/user/me")]
    [ProducesResponseType(typeof(IReadOnlyList<ModelInferenceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInferences(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var inferences = await _useCases.GetInferencesByUserIdAsync(userId, cancellationToken);
        return Ok(inferences);
    }

    [HttpGet("inferences/{id:guid}")]
    [ProducesResponseType(typeof(ModelInferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInference(Guid id, CancellationToken cancellationToken = default)
    {
        var inference = await _useCases.GetInferenceByIdAsync(id, cancellationToken);
        if (inference == null)
            return NotFound($"Инференс с ID {id} не найден");
        return Ok(inference);
    }

    [HttpPut("inferences/{id:guid}/intervene")]
    [ProducesResponseType(typeof(ModelInferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UserIntervention(Guid id, [FromBody] UserInterventionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var inference = await _useCases.UpdateInferenceWithUserInterventionAsync(id, dto.AdjustedSpeed, dto.AdjustedPosition, userId, cancellationToken);
        return Ok(inference);
    }

    [HttpPut("inferences/{id:guid}/actual")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModelInferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActualValues(Guid id, [FromBody] ActualValuesDto dto, CancellationToken cancellationToken = default)
    {
        var inference = await _useCases.UpdateInferenceWithActualValuesAsync(id, dto.ActualSpeed, dto.ActualPosition, cancellationToken);
        return Ok(inference);
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ModelStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetModelStatus()
    {
        return Ok(new ModelStatusDto(_useCases.IsModelLoaded, _useCases.ModelVersion));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }
}

public record MetricsCalculationRequest(ModelInputDto Input, ModelOutputDto Output, double? ActualSpeed, double? ActualPosition);
public record UserInterventionDto(double AdjustedSpeed, double AdjustedPosition);
public record ActualValuesDto(double ActualSpeed, double ActualPosition);
public record ModelStatusDto(bool IsLoaded, string Version);
