using System.Threading.Tasks;

namespace Gameboard.Api.Auth;

internal class ApiKeyAuthenticationService : IApiKeyAuthenticationService
{
    public Task<string> ResolveApiKey(string key)
    {
        throw new System.NotImplementedException();
    }
}
