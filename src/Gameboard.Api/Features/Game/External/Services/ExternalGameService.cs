using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameService
{
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
    Task UpdateGameDeployStatus(string gameId, ExternalGameDeployStatus status, CancellationToken cancellationToken);
    Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameDeployStatus status, CancellationToken cancellationToken);
    Task UpdateTeamExternalUrl(string teamId, string url, CancellationToken cancellationToken);
}

internal class ExternalGameService : IExternalGameService, INotificationHandler<GameResourcesDeployStartNotification>, INotificationHandler<GameResourcesDeployEndNotification>
{
    private readonly IGuidService _guids;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;

    public ExternalGameService
    (
        IGuidService guids,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService
    )
    {
        _guids = guids;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
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

    public async Task Handle(GameResourcesDeployEndNotification notification, CancellationToken cancellationToken)
    {
        await UpdateTeamDeployStatus(notification.TeamIds, ExternalGameDeployStatus.Deployed, cancellationToken);
    }

    public async Task UpdateGameDeployStatus(string gameId, ExternalGameDeployStatus status, CancellationToken cancellationToken)
    {
        await _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(d => d.GameId == gameId)
            .ExecuteUpdateAsync(up => up.SetProperty(d => d.DeployStatus, status));
    }

    public Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameDeployStatus status, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => teamIds.Contains(t.TeamId))
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.DeployStatus, status), cancellationToken);


    public async Task UpdateTeamExternalUrl(string teamId, string url, CancellationToken cancellationToken)
    {
        await _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => t.TeamId == teamId)
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.ExternalGameUrl, url), cancellationToken);
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
}
