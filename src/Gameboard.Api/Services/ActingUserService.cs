using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Services;

public interface IActingUserService
{
    User Get();
}

internal class ActingUserService : IActingUserService
{
    private readonly User _actingUser;

    public ActingUserService(IHttpContextAccessor httpContextAccessor)
    {
        _actingUser = httpContextAccessor.HttpContext.User.ToActor();
    }

    public User Get()
    {
        return _actingUser;
    }
}
