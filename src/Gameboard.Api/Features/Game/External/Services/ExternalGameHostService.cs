using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameHostService
{
    Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationTokena);
    IQueryable<GetExternalGameHostsResponseHost> GetHosts();
    Task<HttpResponseMessage> PingHost(string hostId, CancellationToken cancellationToken);
    Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(IEnumerable<string> teamIds, CalculatedSessionWindow session, CancellationToken cancellationToken);
}

internal class ExternalGameHostService : IExternalGameHostService
{
    private readonly IGameEngineService _gameEngine;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonService _jsonService;
    private readonly ILogger<ExternalGameHostService> _logger;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ExternalGameHostService
    (
        IGameEngineService gameEngine,
        IHttpClientFactory httpClientFactory,
        IJsonService jsonService,
        ILogger<ExternalGameHostService> logger,
        INowService now,
        IStore store,
        ITeamService teamService
    ) =>
    (
        _gameEngine,
        _httpClientFactory,
        _jsonService,
        _logger,
        _now,
        _store,
        _teamService
    ) = (gameEngine, httpClientFactory, jsonService, logger, now, store, teamService);

    public async Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationToken)
    {
        // resolve the team's game and the external host's "extend session" url
        var gameId = await _teamService.GetGameId(teamId, cancellationToken);
        var config = await LoadConfig(gameId, cancellationToken);

        if (config.TeamExtendedEndpoint.IsEmpty())
        {
            _logger.LogInformation($"No team extension configured for the external game host. Skipping session extension to {newSessionEnd} for team {teamId}.");
            return;
        }

        var extendEndpoint = $"{config.TeamExtendedEndpoint}/{teamId}";

        // make the request to the external game host
        _logger.LogInformation($"Posting a team extension ({newSessionEnd}) to external game host at {extendEndpoint}.");
        var client = CreateHttpClient(gameId, config);

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

    public IQueryable<GetExternalGameHostsResponseHost> GetHosts()
        => _store
            .WithNoTracking<ExternalGameHost>()
            .Select(h => new GetExternalGameHostsResponseHost
            {
                Id = h.Id,
                Name = h.Name,
                ClientUrl = h.ClientUrl,
                DestroyResourcesOnDeployFailure = h.DestroyResourcesOnDeployFailure,
                GamespaceDeployBatchSize = h.GamespaceDeployBatchSize,
                HostApiKey = h.HostApiKey,
                HostUrl = h.HostUrl,
                PingEndpoint = h.PingEndpoint,
                StartupEndpoint = h.StartupEndpoint,
                TeamExtendedEndpoint = h.TeamExtendedEndpoint,
                UsedByGames = h.UsedByGames.Select(g => new SimpleEntity
                {
                    Id = g.Id,
                    Name = g.Name
                })
            })
            .OrderBy(h => h.Name);

    public async Task<HttpResponseMessage> PingHost(string hostId, CancellationToken cancellationToken)
    {
        var host = await _store
            .WithNoTracking<ExternalGameHost>()
            .Select(h => new
            {
                h.Id,
                h.PingEndpoint
            })
            .SingleAsync(h => h.Id == hostId, cancellationToken);

        var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetAsync(host.PingEndpoint, cancellationToken);
    }

    public async Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(IEnumerable<string> teamIds, CalculatedSessionWindow session, CancellationToken cancellationToken)
    {
        var metaData = await BuildExternalGameMetaData(teamIds, session, cancellationToken);
        var config = await LoadConfig(metaData.Game.Id, cancellationToken);
        var client = CreateHttpClient(metaData.Game.Id, config);

        _logger.LogInformation($"Posting startup data to to the external game host at {client.BaseAddress}/{config.StartupEndpoint}: {_jsonService.Serialize(metaData)}");
        var teamConfigResponse = await client
            .PostAsJsonAsync(config.StartupEndpoint, metaData, cancellationToken)
            .WithContentDeserializedAs<IDictionary<string, string>>();
        _logger.LogInformation($"Posted startup data. External host's response: {teamConfigResponse} ");

        _logger.LogInformation($"Updating external host URLs for teams...");
        foreach (var teamId in teamConfigResponse.Keys)
            await _store
                .WithNoTracking<ExternalGameTeam>()
                .Where(t => t.Id == teamId)
                .ExecuteUpdateAsync(up => up.SetProperty(t => t.ExternalGameUrl, teamConfigResponse[teamId]), cancellationToken);

        return teamConfigResponse.Keys.Select(key => new ExternalGameClientTeamConfig
        {
            TeamID = key,
            HeadlessServerUrl = teamConfigResponse[key]
        });
    }

    private async Task<ExternalGameStartMetaData> BuildExternalGameMetaData(IEnumerable<string> teamIds, CalculatedSessionWindow session, CancellationToken cancellationToken)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();

        var teamData = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.GameId,
                p.TeamId,
                p.Role,
                p.UserId
            })
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        var teamChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => teamData.Keys.Contains(c.TeamId))
            .Select(c => new
            {
                c.TeamId,
                c.Id,
                c.State
            })
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        foreach (var teamId in teamData.Keys)
        {
            if (!teamChallenges.TryGetValue(teamId, out var challenges))
                throw new InvalidOperationException($"Team {teamId} has no challenges.");

            var gamespaces = challenges.Select(c => new { c.Id, State = _jsonService.Deserialize<GameEngineGameState>(c.State) }).ToArray();
            var players = teamData[teamId];
            var captain = players.First(p => p.Role == PlayerRole.Manager);

            teamsToReturn.Add(new ExternalGameStartMetaDataTeam
            {
                Id = teamId,
                Name = captain.ApprovedName,
                Gamespaces = gamespaces.Select(gs => new ExternalGameStartTeamGamespace
                {
                    Id = gs.Id,
                    VmUris = _gameEngine.GetGamespaceVms(gs.State).Select(vm => vm.Url),
                    IsDeployed = gs.State.HasDeployedGamespace
                }),
                Players = players.Select(p => new ExternalGameStartMetaDataPlayer
                {
                    PlayerId = p.Id,
                    UserId = p.UserId
                })
            });
        }

        var gameId = teamData.Values.SelectMany(p => p.Select(thing => thing.GameId)).Single();
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .SingleAsync(cancellationToken);

        var retVal = new ExternalGameStartMetaData
        {
            Game = game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = session.Start,
                SessionEnd = session.End
            },
            Teams = teamsToReturn
        };

        var metadataJson = _jsonService.Serialize(retVal);
        _logger.LogInformation($"""EXTERNAL GAME: Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}.""");
        return retVal;
    }

    private async Task<ExternalGameHost> LoadConfig(string gameId, CancellationToken cancellationToken)
    {
        var externalConfig = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId && g.Mode == GameEngineMode.External)
            .Select(g => g.ExternalHost)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new ResourceNotFound<ExternalGameHost>(gameId, $"Couldn't locate an ExternalGameConfig for game ID {gameId} - is it set to External mode?");

        return externalConfig;
    }

    private HttpClient CreateHttpClient(string gameId, ExternalGameHost config)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        // if timeout configured use it instead
        if (config.HttpTimeoutInSeconds is not null)
            client.Timeout = TimeSpan.FromSeconds(config.HttpTimeoutInSeconds.Value);

        // todo: different header names? non-bearer?
        if (config.HostApiKey.IsNotEmpty())
            client.DefaultRequestHeaders.Add("x-api-key", config.HostApiKey);

        // startup endpoint, at minimum, is required
        if (config.HostUrl.IsEmpty())
            throw new EmptyExternalStartupEndpoint(gameId, config.StartupEndpoint);

        var hostUrl = config.HostUrl;
        if (!config.HostUrl.EndsWith('/'))
            hostUrl = config.HostUrl + '/';

        client.BaseAddress = new Uri(hostUrl);
        return client;
    }
}
