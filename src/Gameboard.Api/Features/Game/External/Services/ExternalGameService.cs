using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameService
{
    // TODO: don't pass session stuff, centralize
    Task<ExternalGameStartMetaData> BuildExternalGameMetaData(GameResourcesDeployResults resources, DateTimeOffset sessionStart, DateTimeOffset sessionEnd);
    Task CreateTeams(IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task DeleteTeamExternalData(CancellationToken cancellationToken, params string[] teamIds);
    Task<ExternalGameState> GetExternalGameState(string gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the metadata for a given team in an external game. Note that this function will return the
    /// "Not Started" status for teams which have never played the game and thus have no metadata. No error
    /// codes are given in this case.
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ExternalGameTeam> GetTeam(string teamId, CancellationToken cancellationToken);
    Task Start(IEnumerable<string> teamIds, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken);
    Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameDeployStatus status, CancellationToken cancellationToken);
}

internal class ExternalGameService : IExternalGameService, INotificationHandler<GameResourcesDeployStartNotification>, INotificationHandler<GameResourcesDeployEndNotification>
{
    private readonly IGameEngineService _gameEngine;
    private readonly IExternalGameHostService _gameHost;
    private readonly IGameResourcesDeploymentService _gameResources;
    private readonly IGuidService _guids;
    private readonly IJsonService _json;
    private readonly ILogger<ExternalGameService> _logger;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;

    public ExternalGameService
    (
        IGameEngineService gameEngine,
        IExternalGameHostService gameHost,
        IGameResourcesDeploymentService gameResources,
        IGuidService guids,
        IJsonService json,
        ILogger<ExternalGameService> logger,
        INowService now,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService
    )
    {
        _gameEngine = gameEngine;
        _gameHost = gameHost;
        _gameResources = gameResources;
        _guids = guids;
        _json = json;
        _logger = logger;
        _now = now;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
    }

    public async Task<ExternalGameStartMetaData> BuildExternalGameMetaData(GameResourcesDeployResults resources, DateTimeOffset sessionBegin, DateTimeOffset sessionEnd)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();

        // each key is a team
        foreach (var teamId in resources.TeamChallenges.Keys)
        {
            var teamChallenges = resources.TeamChallenges[teamId];
            var teamGamespaces = resources.TeamChallenges[teamId].Select(c => c.State).ToArray();
            var team = await _teamService.GetTeam(teamId);
            var teamPlayers = team.Members.Select(p => new ExternalGameStartMetaDataPlayer
            {
                PlayerId = p.Id,
                UserId = p.UserId
            }).ToArray();

            var teamToReturn = new ExternalGameStartMetaDataTeam
            {
                Id = teamId,
                Name = team.ApprovedName,
                Gamespaces = teamGamespaces.Select(gs => new ExternalGameStartTeamGamespace
                {
                    Id = gs.Id,
                    VmUris = _gameEngine.GetGamespaceVms(gs).Select(vm => vm.Url),
                    IsDeployed = gs.HasDeployedGamespace
                }),
                Players = teamPlayers
            };

            teamsToReturn.Add(teamToReturn);
        }

        var retVal = new ExternalGameStartMetaData
        {
            Game = resources.Game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = sessionBegin,
                SessionEnd = sessionEnd
            },
            Teams = teamsToReturn
        };

        var metadataJson = _json.Serialize(retVal);
        Log($"""Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}.""", retVal.Game.Id);
        return retVal;
    }

    public async Task CreateTeams(IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var teamGameIds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(kv => kv.Key, kv => kv.Select(p => p.GameId), cancellationToken);

        if (teamGameIds.Values.Any(gIds => gIds.Count() > 1))
            throw new InvalidOperationException("One of the teams to be created is tied to more than one game.");

        // first, delete any metadata associated with a previous attempt
        await DeleteTeamExternalData(cancellationToken, teamIds.ToArray());

        // then create an entry for each team in this game
        await _store.SaveAddRange(teamGameIds.Select(teamIdGameId => new ExternalGameTeam
        {
            Id = _guids.GetGuid(),
            GameId = teamIdGameId.Value.Single(),
            TeamId = teamIdGameId.Key,
            DeployStatus = ExternalGameDeployStatus.NotStarted
        }).ToArray());
    }

    public Task DeleteTeamExternalData(CancellationToken cancellationToken, params string[] teamIds)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => teamIds.Contains(t.TeamId))
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<ExternalGameState> GetExternalGameState(string gameId, CancellationToken cancellationToken)
    {
        var gameData = await _store
            .WithNoTracking<Data.Game>()
            .Include(g => g.ExternalGameTeams)
            .Include(g => g.Specs)
            .Include(g => g.Players)
                .ThenInclude(p => p.Sponsor)
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .SingleAsync(g => g.Id == gameId, cancellationToken);

        var specIds = gameData.Specs.Select(s => s.Id).ToArray();
        var teamIds = gameData.Players.Select(p => p.TeamId).Distinct();

        // get challenges separately because of SpecId nonsense
        // group by TeamId for quicker lookups
        var teamChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => specIds.Contains(c.SpecId))
            .Where(c => teamIds.Contains(c.TeamId))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.ToArray(), cancellationToken);

        // Compute hasStandardSessionWindow: do all players start and end at the same time
        // as all challenges?
        var startDates = teamChallenges
            .SelectMany(c => c.Value)
            .Select(c => c.StartTime)
            .Concat(gameData.Players.Select(p => p.SessionBegin))
            .Distinct()
            .ToArray();
        var endDates = teamChallenges
            .SelectMany(c => c.Value)
            .Select(c => c.EndTime)
            .Concat(gameData.Players.Select(p => p.SessionEnd))
            .Distinct()
            .ToArray();

        // this expresses whether there are any player session dates or
        // challenge start/end times that are misaligned - only really matters
        // once the game has started
        var hasStandardSessionWindow = startDates.Length > 1 && endDates.Length > 1;

        // if we have a standardized session window, send it down with the data
        DateTimeOffset? overallStart = null;
        DateTimeOffset? overallEnd = null;
        if (startDates.Length == 1 && endDates.Length == 1)
        {
            overallStart = startDates.Single();
            overallEnd = endDates.Single();
        }

        // compute teams
        var teams = gameData.Players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var captains = teams.Keys
            .ToDictionary(key => key, key => _teamService.ResolveCaptain(teams[key]));

        var teamDeployStatuses = new Dictionary<string, ExternalGameDeployStatus>();

        foreach (var teamId in teams.Keys)
        {
            if (gameData.ExternalGameTeams.Any(t => t.ExternalGameUrl is null))
            {
                teamDeployStatuses.Add(teamId, ExternalGameDeployStatus.PartiallyDeployed);
                continue;
            }

            if (gameData.ExternalGameTeams.Any(t => t.TeamId == teamId && t.DeployStatus == ExternalGameDeployStatus.Deploying))
            {
                teamDeployStatuses.Add(teamId, ExternalGameDeployStatus.Deploying);
                continue;
            }

            if (!teamChallenges.TryGetValue(teamId, out Data.Challenge[] value) || !value.Any())
            {
                teamDeployStatuses.Add(teamId, ExternalGameDeployStatus.NotStarted);
                continue;
            }

            var allChallengesCreated = specIds.All(specId => teamChallenges[teamId].Any(c => c.SpecId == specId));
            var allGamespacesDeployedOrFinished = teamChallenges[teamId].All(c => c.HasDeployedGamespace || c.Score >= c.Points);

            if (allChallengesCreated && allGamespacesDeployedOrFinished)
            {
                teamDeployStatuses.Add(teamId, ExternalGameDeployStatus.Deployed);
                continue;
            }

            throw new CantResolveTeamDeployStatus(gameId, teamId);
        }

        // and their readiness
        var playerReadiness = new Dictionary<string, bool>();
        if (gameData.RequireSynchronizedStart)
        {
            var syncStartState = await _syncStartGameService.GetSyncStartState(gameId, cancellationToken);
            playerReadiness = syncStartState.Teams
                .SelectMany(t => t.Players)
                .ToDictionary(p => p.Id, p => p.IsReady);
        }

        // the game's overall deploy status is the lowest value of all teams' deploy statuses
        var overallDeployState = ResolveOverallDeployStatus(teamDeployStatuses.Values);

        return new ExternalGameState
        {
            Game = new SimpleEntity { Id = gameData.Id, Name = gameData.Name },
            OverallDeployStatus = overallDeployState,
            Specs = gameData.Specs.Select(s => new SimpleEntity { Id = s.Id, Name = s.Name }).OrderBy(s => s.Name),
            HasNonStandardSessionWindow = hasStandardSessionWindow,
            StartTime = overallStart,
            EndTime = overallEnd,
            Teams = teams.Keys.Select(key => new ExternalGameStateTeam
            {
                Id = captains[key].TeamId,
                Name = captains[key].ApprovedName,
                DeployStatus = teamDeployStatuses.TryGetValue(key, out ExternalGameDeployStatus value) ? value : ExternalGameDeployStatus.NotStarted,
                IsReady = teams[key].All(p => p.IsReady),
                Challenges = gameData.Specs.Select(s =>
                {
                    // note that the team may not have any challenges and thus not be
                    // in the challenge dictionary. If they aren't, use default
                    // values
                    string id = null;
                    var challengeCreated = false;
                    var gamespaceDeployed = false;
                    string specId = s.Id;
                    DateTimeOffset? startTime = null;
                    DateTimeOffset? endTime = null;

                    if (teamChallenges.TryGetValue(key, out Data.Challenge[] value))
                    {
                        var challenge = value.SingleOrDefault(c => c.SpecId == s.Id);

                        id = challenge?.Id;
                        challengeCreated = challenge is not null;
                        gamespaceDeployed = challenge is not null & challenge.HasDeployedGamespace;
                        startTime = challenge?.StartTime;
                        endTime = challenge?.EndTime;
                    }

                    return new ExternalGameStateChallenge
                    {
                        Id = id,
                        ChallengeCreated = challengeCreated,
                        GamespaceDeployed = gamespaceDeployed,
                        SpecId = specId,
                        StartTime = startTime,
                        EndTime = endTime
                    };
                }),
                Players = teams[key].Select(p => new ExternalGameStatePlayer
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsCaptain = captains[key].Id == p.Id,
                    Sponsor = new SimpleSponsor
                    {
                        Id = p.SponsorId,
                        Name = p.Sponsor.Name,
                        Logo = p.Sponsor.Logo
                    },
                    // TODO: hack because this is no longer just about external/sync games
                    Status = !playerReadiness.Any() ? ExternalGameStatePlayerStatus.NotConnected :
                        playerReadiness[p.Id] ?
                            ExternalGameStatePlayerStatus.Ready :
                            ExternalGameStatePlayerStatus.NotReady,
                    User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName }
                })
                .OrderByDescending(p => p.IsCaptain)
                .ThenBy(p => p.Name),
                Sponsors = teams[key].Select(p => new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }),
            })
            .OrderBy(t => t.Name)
                .ThenBy(t => t.Players.Count())
        };
    }

    public Task<ExternalGameTeam> GetTeam(string teamId, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .SingleOrDefaultAsync(r => r.TeamId == teamId, cancellationToken);

    public async Task Handle(GameResourcesDeployStartNotification notification, CancellationToken cancellationToken)
    {
        await CreateTeams(notification.TeamIds, cancellationToken);
        await UpdateTeamDeployStatus(notification.TeamIds, ExternalGameDeployStatus.Deploying, cancellationToken);
    }

    public Task Handle(GameResourcesDeployEndNotification notification, CancellationToken cancellationToken)
        => UpdateTeamDeployStatus(notification.TeamIds, ExternalGameDeployStatus.Deployed, cancellationToken);

    public async Task Start(IEnumerable<string> teamIds, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken)
    {
        var resources = await _gameResources.DeployResources(teamIds, cancellationToken);

        // update external host and get configuration information for teams
        Log("Notifying external game host...", resources.Game.Id);
        // build metadata for external host
        var metaData = await BuildExternalGameMetaData(resources, sessionWindow.Start, sessionWindow.End);
        var externalHostTeamConfigs = await _gameHost.StartGame(metaData, cancellationToken);
        Log($"External host team configurations: {_json.Serialize(externalHostTeamConfigs)}", resources.Game.Id);
        Log("External game host notified!", resources.Game.Id);

        // then assign a headless server to each team
        foreach (var teamId in resources.TeamChallenges.Keys)
        {
            var config = externalHostTeamConfigs.SingleOrDefault(t => t.TeamID == teamId);
            if (config is null)
                Log($"Team {teamId} wasn't assigned a headless URL by the external host (Gamebrain).", resources.Game.Id);
            else
            {
                // TODO: prolly need to see about sending signalR notification
                // but also record it in the DB in case someone cache clears or rejoins from a different machine/browser
                await UpdateTeamExternalUrl(teamId, config.HeadlessServerUrl, cancellationToken);
            }
        }
    }

    public Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameDeployStatus status, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => teamIds.Contains(t.TeamId))
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.DeployStatus, status), cancellationToken);

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }

    private ExternalGameDeployStatus ResolveOverallDeployStatus(IEnumerable<ExternalGameDeployStatus> teamStatuses)
    {
        if (teamStatuses.All(s => s == ExternalGameDeployStatus.Deployed))
            return ExternalGameDeployStatus.Deployed;

        if (teamStatuses.All(s => s == ExternalGameDeployStatus.NotStarted) || !teamStatuses.Any())
            return ExternalGameDeployStatus.NotStarted;

        if (teamStatuses.Any(s => s == ExternalGameDeployStatus.Deploying))
            return ExternalGameDeployStatus.Deploying;

        return ExternalGameDeployStatus.PartiallyDeployed;
    }

    private Task UpdateTeamExternalUrl(string teamId, string url, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => t.TeamId == teamId)
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.ExternalGameUrl, url), cancellationToken);
}
