using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.Games.External;

internal interface IExternalGameHostAccessTokenProvider
{
    Task<string> GetToken();
}

internal class HttpContextAccessTokenProvider : IExternalGameHostAccessTokenProvider
{
    private readonly BackgroundTaskContext _backgroundTaskContext;
    private HttpContext _httpContext;

    public HttpContextAccessTokenProvider(BackgroundTaskContext backgroundTaskContext, IHttpContextAccessor httpContextAccessor)
    {
        _backgroundTaskContext = backgroundTaskContext;
        _httpContext = httpContextAccessor.HttpContext;
    }

    public async Task<string> GetToken()
    {
        if (_httpContext is not null)
            return await _httpContext.GetTokenAsync("access_token");

        return _backgroundTaskContext.AccessToken;
    }
}
