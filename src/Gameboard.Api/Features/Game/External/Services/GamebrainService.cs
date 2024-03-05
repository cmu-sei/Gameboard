using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.UnityGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IGamebrainService
{
    Task<string> DeployUnitySpace(string gameId, string teamId);
    Task<string> GetGameState(string gameId, string teamId);
    Task<string> UndeployUnitySpace(string gameId, string teamId);
    Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms);
    Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData);
    Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationTokena);
}

internal class GamebrainService : IGamebrainService
{
    private readonly IExternalGameHostAccessTokenProvider _accessTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonService _jsonService;
    private readonly ILogger<GamebrainService> _logger;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public GamebrainService
    (
        IExternalGameHostAccessTokenProvider accessTokenProvider,
        IHttpClientFactory httpClientFactory,
        IJsonService jsonService,
        ILogger<GamebrainService> logger,
        IStore store,
        ITeamService teamService
    ) =>
    (
        _accessTokenProvider,
        _httpClientFactory,
        _jsonService,
        _logger,
        _store,
        _teamService
    ) = (accessTokenProvider, httpClientFactory, jsonService, logger, store, teamService);

    public async Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationToken)
    {
        // resolve the team's game and the external host's "extend session" url
        var gameId = await _teamService.GetGameId(teamId, cancellationToken);
        var rawExtendEndpoint = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => g.ExternalGameTeamExtendedEndpoint)
            .SingleOrDefaultAsync(cancellationToken);

        if (rawExtendEndpoint.IsEmpty())
        {
            _logger.LogInformation($"No team extension configured for the external game host. Skipping session extension to {newSessionEnd} for team {teamId}");
            return;
        }

        var extendEndpoint = $"{rawExtendEndpoint.Trim()}/{teamId}";

        // make the request to the external game host
        _logger.LogInformation($"Posting a team extension ({newSessionEnd}) to external game host at {extendEndpoint}.");
        var client = await CreateGamebrain();

        try
        {
            var response = await client
            .PutAsJsonAsync(extendEndpoint, new { NewSessionEnd = newSessionEnd }, cancellationToken);

            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully extended the team.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"""The external gamehost for game {gameId} is configured with a "team extend" endpoint at {extendEndpoint}, but the request to it failed ({ex.GetType().Name} :: {ex.Message}).""");
        }
    }

    public async Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData)
    {
        // resolve and format the endpoint to post the request to
        var rawStartupUrl = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == metaData.Game.Id)
            .Select(g => g.ExternalGameStartupEndpoint)
            .SingleOrDefaultAsync();

        if (rawStartupUrl.IsEmpty())
            throw new EmptyExternalStartupEndpoint(metaData.Game.Id, rawStartupUrl);

        var startupUrl = rawStartupUrl.Trim();

        // create a client and post the data
        var client = await CreateGamebrain();

        _logger.LogInformation($"Posting startup data to to the external game host at {client.BaseAddress}/{startupUrl}: {_jsonService.Serialize(metaData)}");
        var teamConfigResponse = await client
            .PostAsJsonAsync($"{startupUrl}", metaData)
            .WithContentDeserializedAs<IDictionary<string, string>>();
        _logger.LogInformation($"Posted startup data. External host's response: {teamConfigResponse} ");

        return teamConfigResponse.Keys.Select(key => new ExternalGameClientTeamConfig
        {
            TeamID = key,
            HeadlessServerUrl = teamConfigResponse[key]
        });
    }

    private async Task<HttpClient> CreateGamebrain()
    {
        var gb = _httpClientFactory.CreateClient("Gamebrain");
        gb.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _accessTokenProvider.GetToken()}");
        return gb;
    }

    [Obsolete("PC4")]
    public async Task<string> DeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();
        var m = await client.PostAsync($"admin/deploy/{gameId}/{teamId}", null);
        return await m.Content.ReadAsStringAsync();
    }

    [Obsolete("PC4")]
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

    [Obsolete("PC4")]
    public async Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms)
    {
        var client = await CreateGamebrain();
        await client.PostAsync($"admin/update_console_urls/{teamId}", JsonContent.Create(vms.ToArray(), mediaType: MediaTypeHeaderValue.Parse("application/json")));
    }

    [Obsolete("PC4")]
    public async Task<string> UndeployUnitySpace(string gameId, string teamId)
    {
        var client = await CreateGamebrain();

        var m = await client.GetAsync($"admin/undeploy/{teamId}");
        return await m.Content.ReadAsStringAsync();
    }
}
