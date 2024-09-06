using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common.Services;

public interface IActingUserService
{
    User Get();
}

internal class ActingUserService(
    BackgroundAsyncTaskContext backgroundTaskContext,
    IHttpContextAccessor httpContextAccessor
    ) : IActingUserService
{
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext = backgroundTaskContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public User Get()
    {
        return _httpContextAccessor?.HttpContext?.Items[AppConstants.RequestContextGameboardUser] as User ?? _backgroundTaskContext?.ActingUser;
    }
}
