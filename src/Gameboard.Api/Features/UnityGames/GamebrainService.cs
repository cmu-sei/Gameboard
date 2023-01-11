using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.UnityGames;

internal class GamebrainService : IGamebrainService
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public GamebrainService(
        IAccessTokenProvider accessTokenProvider,
        IHttpClientFactory httpClientFactory)
    {
        _accessTokenProvider = accessTokenProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> DeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();
        var m = await client.PostAsync($"admin/deploy/{gameId}/{teamId}", null);
        return await m.Content.ReadAsStringAsync();
    }

    public async Task<string> GetGameState(string gameId, string teamId)
    {
        var client = await CreateGamebrain();
        var gamebrainEndpoint = $"admin/deploy/{gameId}/{teamId}";
        var m = await client.GetAsync(gamebrainEndpoint);

        if (m.IsSuccessStatusCode)
        {
            var stringContent = await m.Content.ReadAsStringAsync();

            if (!stringContent.IsEmpty())
            {
                return stringContent;
            }

            throw new GamebrainEmptyResponseException(HttpMethod.Get, gamebrainEndpoint);
        }

        throw new GamebrainException(HttpMethod.Get, gamebrainEndpoint, m.StatusCode, await m.Content.ReadAsStringAsync());
    }

    public async Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms)
    {
        var client = await CreateGamebrain();
        await client.PostAsync($"admin/update_console_urls/{teamId}", JsonContent.Create<UnityGameVm[]>(vms.ToArray(), mediaType: MediaTypeHeaderValue.Parse("application/json")));
    }

    public async Task<string> UndeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();

        var m = await client.GetAsync($"admin/undeploy/{teamId}");
        return await m.Content.ReadAsStringAsync();
    }

    private ActionResult BuildError(HttpResponseMessage response, string message)
    {
        var result = new ObjectResult(message);
        result.StatusCode = (int)response.StatusCode;
        return result;
    }

    private async Task<HttpClient> CreateGamebrain()
    {
        var gb = _httpClientFactory.CreateClient("Gamebrain");
        gb.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _accessTokenProvider.GetToken()}");
        return gb;
    }
}
