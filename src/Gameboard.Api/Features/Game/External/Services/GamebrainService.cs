using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Services;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IGamebrainService
{
    Task<string> DeployUnitySpace(string gameId, string teamId);
    Task<string> GetGameState(string gameId, string teamId);
    Task<string> UndeployUnitySpace(string gameId, string teamId);
    Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms);
    Task<IEnumerable<ExternalGameClientTeamConfig>> StartV2Game(ExternalGameStartMetaData metaData);
}

internal class GamebrainService : IGamebrainService
{
    private readonly IExternalGameHostAccessTokenProvider _accessTokenProvider;
    private readonly IGameService _gameService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GamebrainService> _logger;

    public GamebrainService
    (
        IExternalGameHostAccessTokenProvider accessTokenProvider,
        IGameService gameService,
        IHttpClientFactory httpClientFactory
    ) =>
    (
        _accessTokenProvider,
        _gameService,
        _httpClientFactory
    ) = (accessTokenProvider, gameService, httpClientFactory);

    public async Task<string> DeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();
        var m = await client.PostAsync($"admin/deploy/{gameId}/{teamId}", null);
        return await m.Content.ReadAsStringAsync();
    }

    public async Task<string> GetGameState(string gameId, string teamId)
    {
        var client = await CreateGamebrain();
        var startupUrl = await _gameService.ResolveExternalStartupUrl(gameId);
        var gamebrainEndpoint = $"{startupUrl}/{gameId}/{teamId}";
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

    public async Task<IEnumerable<ExternalGameClientTeamConfig>> StartV2Game(ExternalGameStartMetaData metaData)
    {
        var startupUrl = await _gameService.ResolveExternalStartupUrl(metaData.Game.Id);
        var client = await CreateGamebrain();

        _logger.LogInformation($"Posting startup data to Gamebrain at {startupUrl}/{metaData.Game.Id}: {metaData}");
        var teamConfigResponse = await client
            .PostAsJsonAsync($"{startupUrl}", metaData)
            .WithContentDeserializedAs<IDictionary<string, string>>();

        _logger.LogInformation($"Posted startup data. Gamebrain's response: {teamConfigResponse} ");

        return teamConfigResponse.Keys.Select(key => new ExternalGameClientTeamConfig
        {
            TeamID = key,
            HeadlessServerUrl = teamConfigResponse[key]
        });
    }

    public async Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms)
    {
        var client = await CreateGamebrain();
        await client.PostAsync($"admin/update_console_urls/{teamId}", JsonContent.Create(vms.ToArray(), mediaType: MediaTypeHeaderValue.Parse("application/json")));
    }

    public async Task<string> UndeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();

        var m = await client.GetAsync($"admin/undeploy/{teamId}");
        return await m.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> CreateGamebrain()
    {
        var gb = _httpClientFactory.CreateClient("Gamebrain");
        gb.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _accessTokenProvider.GetToken()}");
        return gb;
    }
}
