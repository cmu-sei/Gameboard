using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.Games.External;

internal interface IExternalGameHostAccessTokenProvider
{
    Task<string> GetToken();
}

internal class HttpContextAccessTokenProvider : IExternalGameHostAccessTokenProvider
{
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private HttpContext _httpContext;

    public HttpContextAccessTokenProvider(BackgroundAsyncTaskContext backgroundTaskContext, IHttpContextAccessor httpContextAccessor)
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
