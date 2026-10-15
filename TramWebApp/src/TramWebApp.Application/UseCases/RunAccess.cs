namespace TramWebApp.Application.UseCases;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
public class RunAccess(ITramRunRepository runs, ICurrentUser user) : IRunAccess
{
    public async Task<TramRun> RequireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await runs.GetByIdAsync(id, cancellationToken) ?? throw new EntityNotFoundException("TramRun", id);
        if (user.Id == null || (!user.IsAdmin && run.CreatedByUserId != user.Id))
            throw new UnauthorizedAccessException("Нет доступа к данным этого прогона.");
        return run;
    }
}
