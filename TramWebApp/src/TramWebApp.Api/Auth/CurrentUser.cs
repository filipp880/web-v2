namespace TramWebApp.Api.Auth;
using System.Security.Claims;
using TramWebApp.Application.Interfaces;
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}
