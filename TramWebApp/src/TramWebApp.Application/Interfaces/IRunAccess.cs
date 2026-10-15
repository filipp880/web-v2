namespace TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
public interface ICurrentUser { Guid? Id { get; } bool IsAdmin { get; } }
public interface IRunAccess { Task<TramRun> RequireAsync(Guid id, CancellationToken cancellationToken = default); }
