namespace TramWebApp.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthUseCases _useCases;

    public AuthController(IAuthUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _useCases.LoginAsync(dto, cancellationToken);
        if (result == null)
            return Unauthorized("Неверное имя пользователя или пароль");
        return Ok(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _useCases.RegisterAsync(dto, cancellationToken);
        if (user == null)
            return BadRequest("Пользователь с таким именем или email уже существует");
        return CreatedAtAction(nameof(GetProfile), null, user);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var profile = await _useCases.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
            return NotFound("Пользователь не найден");
        return Ok(profile);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }
}