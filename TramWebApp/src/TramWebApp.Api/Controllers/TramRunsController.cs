namespace TramWebApp.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Application.Common;

[ApiController]
[Route("api/tram-runs")]
[Authorize]
public class TramRunsController : ControllerBase
{
    private readonly ITramRunUseCases _useCases;

    public TramRunsController(ITramRunUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TramRunListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        
        var runs = isAdmin 
            ? await _useCases.GetAllAsync(cancellationToken)
            : await _useCases.GetByUserIdAsync(userId, cancellationToken);
        
        return Ok(runs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TramRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await _useCases.GetByIdAsync(id, cancellationToken);
        if (run == null)
            return NotFound($"Прогон с ID {id} не найден");

        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        
        if (!isAdmin && run.CreatedByUserId != userId)
            return Forbid();

        return Ok(run);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TramRunDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTramRunDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var run = await _useCases.CreateAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = run.Id }, run);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TramRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTramRunDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        
        var run = await _useCases.GetByIdAsync(id, cancellationToken);
        if (run == null)
            return NotFound($"Прогон с ID {id} не найден");

        if (!isAdmin && run.CreatedByUserId != userId)
            return Forbid();

        var updated = await _useCases.UpdateAsync(id, dto, run.CreatedByUserId, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var run = await _useCases.GetByIdAsync(id, cancellationToken);
        if (run == null) return NotFound();
        await _useCases.DeleteAsync(id, run.CreatedByUserId, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }
}
