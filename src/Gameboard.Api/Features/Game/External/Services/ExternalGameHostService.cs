using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameHostService
{
    IQueryable<GetExternalGameHostsResponseHost> GetHosts();
    Task<HttpResponseMessage> PingHost(string hostId, CancellationToken cancellationToken);
    Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData, CancellationToken cancellationToken);
    Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationTokena);
}

internal class ExternalGameHostService : IExternalGameHostService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonService _jsonService;
    private readonly ILogger<ExternalGameHostService> _logger;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ExternalGameHostService
    (
        IHttpClientFactory httpClientFactory,
        IJsonService jsonService,
        ILogger<ExternalGameHostService> logger,
        IStore store,
        ITeamService teamService
    ) =>
    (
        _httpClientFactory,
        _jsonService,
        _logger,
        _store,
        _teamService
    ) = (httpClientFactory, jsonService, logger, store, teamService);

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

    public async Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData, CancellationToken cancellationToken)
    {
        var config = await LoadConfig(metaData.Game.Id, cancellationToken);
        var client = CreateHttpClient(metaData.Game.Id, config);

        _logger.LogInformation($"Posting startup data to to the external game host at {client.BaseAddress}/{config.StartupEndpoint}: {_jsonService.Serialize(metaData)}");
        var teamConfigResponse = await client
            .PostAsJsonAsync(config.StartupEndpoint, metaData, cancellationToken)
            .WithContentDeserializedAs<IDictionary<string, string>>();
        _logger.LogInformation($"Posted startup data. External host's response: {teamConfigResponse} ");

        return teamConfigResponse.Keys.Select(key => new ExternalGameClientTeamConfig
        {
            TeamID = key,
            HeadlessServerUrl = teamConfigResponse[key]
        });
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

        // todo: different header names? non-bearer?
        if (config.HostApiKey.IsNotEmpty())
            client.DefaultRequestHeaders.Add("x-api-key", config.HostApiKey);

        // startup endpoint, at minimum, is required
        if (config.HostUrl.IsEmpty())
            throw new EmptyExternalStartupEndpoint(gameId, config.StartupEndpoint);

        var hostUrl = config.HostUrl.Trim().TrimEnd('/');
        client.BaseAddress = new Uri(hostUrl);
        return client;
    }
}
