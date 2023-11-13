using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common.Services;

public interface IActingUserService
{
    User Get();
}

internal class ActingUserService : IActingUserService
{
    private readonly User _actingUser = null;

    public ActingUserService(BackgroundTaskContext backgroundTaskContext, IHttpContextAccessor httpContextAccessor)
    {
        _actingUser = httpContextAccessor?.HttpContext?.User?.ToActor();
        _actingUser ??= backgroundTaskContext.ActingUser;
    }

    public User Get()
    {
        return _actingUser;
    }
}
