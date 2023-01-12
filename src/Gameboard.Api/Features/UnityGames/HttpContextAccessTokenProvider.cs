using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.UnityGames;

internal class HttpContextAccessTokenProvider : IAccessTokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAccessTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string> GetToken()
    {
        return _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
    }
}
