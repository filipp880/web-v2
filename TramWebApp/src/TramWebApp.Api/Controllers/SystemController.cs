namespace TramWebApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TramWebApp.Domain.Enums;

[ApiController, AllowAnonymous, Route("api/system")]
public class SystemController(IConfiguration configuration) : ControllerBase
{
    public record StorageInfoDto(string Mode, int? RunCapacity, bool Persistent);
    [HttpGet("storage")]
    [ProducesResponseType(typeof(StorageInfoDto), StatusCodes.Status200OK)]
    public IActionResult Storage()
    {
        var mode = configuration.GetValue<StorageMode>("StorageMode", StorageMode.PostgreSQL);
        return Ok(new StorageInfoDto(mode.ToString(), mode == StorageMode.InMemory ? 50 : null, mode == StorageMode.PostgreSQL));
    }
}
