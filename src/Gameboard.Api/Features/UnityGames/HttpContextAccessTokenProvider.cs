using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.UnityGames;

internal class HttpContextAccessTokenProvider : IAccessTokenProvider
{
    private readonly HttpContext _httpContext;

    public HttpContextAccessTokenProvider(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public Task<string> GetToken()
    {
        return _httpContext.GetTokenAsync("access_token");
    }
}
