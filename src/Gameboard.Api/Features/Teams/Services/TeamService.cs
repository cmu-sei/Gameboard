using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Practice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public interface ITeamService
{
    Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken);
    Task EndSession(string teamId, User actor, CancellationToken cancellationToken);
    Task<Api.Player> ExtendSession(ExtendTeamSessionRequest request, CancellationToken cancellationToken);
    Task<IEnumerable<SimpleEntity>> GetChallengesWithActiveGamespace(string teamId, string gameId, CancellationToken cancellationToken);
    Task<bool> GetExists(string teamId);
    Task<string> GetGameId(string teamId, CancellationToken cancellationToken);
    Task<string> GetGameId(IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task<CalculatedSessionWindow> GetSession(string teamId, CancellationToken cancellationToken);
    Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken);
    Task<Team> GetTeam(string id);
    Task<IEnumerable<Team>> GetTeams(IEnumerable<string> ids);
    Task<string[]> GetUserTeamIds(string userId);
    Task<bool> IsAtGamespaceLimit(string teamId, Data.Game game, CancellationToken cancellationToken);
    Task<bool> IsOnTeam(string teamId, string userId);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken);
    Task<Data.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken);
    Data.Player ResolveCaptain(IEnumerable<Data.Player> players);
    Task<IDictionary<string, Data.Player>> ResolveCaptains(IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task SetSessionWindow(string teamId, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken);
    Task SetSessionWindow(IEnumerable<string> teamIds, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken);
}

internal class TeamService : ITeamService, INotificationHandler<UserJoinedTeamNotification>
{
    private readonly ICacheService _cacheService;
    private readonly IGameEngineService _gameEngine;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IInternalHubBus _teamHubService;
    private readonly IPlayerStore _playerStore;
    private readonly IPracticeService _practiceService;
    private readonly IStore _store;

    public TeamService
    (
        ICacheService cacheService,
        IGameEngineService gameEngine,
        IMapper mapper,
        IMediator mediator,
        INowService now,
        IInternalHubBus teamHubService,
        IPlayerStore playerStore,
        IPracticeService practiceService,
        IStore store
    )
    {
        _cacheService = cacheService;
        _gameEngine = gameEngine;
        _mapper = mapper;
        _mediator = mediator;
        _now = now;
        _playerStore = playerStore;
        _practiceService = practiceService;
        _store = store;
        _teamHubService = teamHubService;
    }

    public Task Handle(UserJoinedTeamNotification notification, CancellationToken cancellationToken)
        => Task.Run(() => _cacheService.Invalidate(GetUserTeamIdsCacheKey(notification.UserId)), cancellationToken);

    public async Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken)
    {
        var teamState = await GetTeamState(teamId, actingUser, cancellationToken);
        var isSyncStartGame = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == teamState.GameId && g.RequireSynchronizedStart)
            .AnyAsync(cancellationToken);

        // delete player records
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .Select(p => new
            {
                p.Id,
                Name = p.ApprovedName,
                p.TeamId,
                p.UserId
            })
            .ToArrayAsync(cancellationToken);
        var playerIds = players.Select(p => p.Id).Distinct().ToArray();

        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => playerIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        // notify app listeners
        await _mediator.Publish(new GameEnrolledPlayersChangeNotification(new GameEnrolledPlayersChangeContext(teamState.GameId, isSyncStartGame)));

        // notify hub that the team is deleted /players left so the client can respond
        await _teamHubService.SendTeamDeleted(teamState, actingUser);
    }

    public async Task EndSession(string teamId, User actor, CancellationToken cancellationToken)
    {
        // for now "end the session" is just set its endtime to now
        var session = await GetSession(teamId, cancellationToken);
        session.End = _now.Get();
        await SetSessionWindow(teamId, session, cancellationToken);
    }

    public async Task<Api.Player> ExtendSession(ExtendTeamSessionRequest request, CancellationToken cancellationToken)
    {
        // find the team and their captain
        var captain = await ResolveCaptain(request.TeamId, cancellationToken);

        // be sure they have an active session before we go extending things
        var currentSession = await GetSession(request.TeamId, cancellationToken) ?? throw new CantExtendUnstartedSession(request.TeamId);

        // in competitive mode, session end is what's requested in the API call
        var finalSessionEnd = request.NewSessionEnd;
        // in practice mode, there's special super secret logic (which is currently that the request results in 
        // a one-hour extension up to a cap defined in settings)
        if (captain.IsPractice)
            finalSessionEnd = await _practiceService.GetExtendedSessionEnd(captain.SessionBegin, captain.SessionEnd, cancellationToken);

        // update the player entities and gamespaces
        currentSession.End = finalSessionEnd;
        await SetSessionWindow(request.TeamId, currentSession, cancellationToken);

        // return the updated session via the captain
        // manually set the new session end here, because this object is stale
        captain.SessionEnd = finalSessionEnd;
        var captainModel = _mapper.Map<Api.Player>(captain);

        // notify listeners of the session extension
        await _mediator.Publish(new TeamSessionExtendedNotification(request.TeamId, request.NewSessionEnd), cancellationToken);

        // update the notifications hub on the client side
        await _teamHubService.SendTeamUpdated(captainModel, request.Actor);
        await _teamHubService.SendTeamSessionExtended(new TeamState
        {
            Id = captain.TeamId,
            ApprovedName = captain.ApprovedName,
            GameId = captain.GameId,
            Name = captain.Name,
            NameStatus = captain.NameStatus,
            SessionBegin = captain.SessionBegin,
            SessionEnd = finalSessionEnd,
            Actor = new SimpleEntity { Id = request.Actor.Id, Name = request.Actor.ApprovedName }

        }, request.Actor);

        return captainModel;
    }

    public async Task<IEnumerable<SimpleEntity>> GetChallengesWithActiveGamespace(string teamId, string gameId, CancellationToken cancellationToken)
        => await _store
            .List<Data.Challenge>()
            .Where(c => c.TeamId == teamId)
            .Where(c => c.GameId == gameId)
            .Where(c => c.HasDeployedGamespace == true)
            .Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })
            .ToArrayAsync(cancellationToken);

    public Task<bool> GetExists(string teamId)
        => _store.WithNoTracking<Data.Player>().AnyAsync(p => p.TeamId == teamId);

    public Task<string> GetGameId(string teamId, CancellationToken cancellationToken)
        => GetGameId(new string[] { teamId }, cancellationToken);

    public async Task<string> GetGameId(IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var gameIds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .Select(p => p.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (gameIds.Length != 1)
            throw new TeamsAreFromMultipleGames(teamIds, gameIds);

        return gameIds.Single();
    }

    public async Task<CalculatedSessionWindow> GetSession(string teamId, CancellationToken cancellationToken)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Select(p => new
            {
                p.SessionBegin,
                p.SessionEnd,
                p.SessionMinutes,
                p.IsLateStart,
                p.TeamId
            })
            .Distinct()
            .Where(p => p.TeamId == teamId)
            .ToArrayAsync(cancellationToken);

        if (!players.Any())
            throw new ResourceNotFound<Team>(teamId);

        if (players.Length > 1)
            throw new PlayersAreFromMultipleTeams(players.Select(p => p.TeamId), $"Couldn't resolve a single session for team {teamId}.");

        var player = players.Single();

        // this is quasi-expected - if a player's end hasn't been set, they don't have a session, and 
        // we communicate that by returning null when it's asked for
        if (player.SessionEnd == DateTimeOffset.MinValue)
            return null;

        return new CalculatedSessionWindow
        {
            Start = player.SessionBegin,
            End = player.SessionEnd,
            IsLateStart = player.IsLateStart,
            LengthInMinutes = player.SessionMinutes
        };
    }

    public async Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken)
    {
        var now = _now.Get();

        return await _playerStore
            .List()
            .CountAsync
            (
                p =>
                    p.GameId == gameId &&
                    p.Role == PlayerRole.Manager &&
                    now < p.SessionEnd,
                cancellationToken
            );
    }

    public async Task<Team> GetTeam(string id)
    {
        var team = await GetTeams(id.ToEnumerable());

        if (team.Count() != 1)
            throw new ResourceNotFound<Team>(id);

        return team.Single();
    }

    public async Task<IEnumerable<Team>> GetTeams(IEnumerable<string> ids)
    {
        var retVal = new List<Team>();
        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Sponsor)
                .Include(p => p.AdvancedFromGame)
                .Include(p => p.AdvancedFromPlayer)
            .Where(p => ids.Contains(p.TeamId))
            .GroupBy(p => p.TeamId, p => p)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray());

        if (teamPlayers.Count == 0)
            return Array.Empty<Team>();

        foreach (var teamId in teamPlayers.Keys)
        {
            var captain = ResolveCaptain(teamPlayers[teamId]);
            var team = _mapper.Map<Team>(captain);

            team.Members = _mapper.Map<TeamMember[]>(teamPlayers[teamId]);
            team.Sponsors = _mapper.Map<Sponsor[]>(teamPlayers[teamId].Select(p => p.Sponsor));

            if (captain.AdvancedFromGame is not null)
            {
                team.AdvancedFromGame = new SimpleEntity { Id = captain.AdvancedFromGameId, Name = captain.AdvancedFromGame.Name };
                team.IsAdvancedFromTeamGame = captain.AdvancedFromGame.IsTeamGame();
            }

            if (captain.AdvancedFromPlayer is not null)
                team.AdvancedFromPlayer = new SimpleEntity { Id = captain.AdvancedFromPlayerId, Name = captain.AdvancedFromPlayer.ApprovedName };

            team.AdvancedFromTeamId = captain.AdvancedFromTeamId;
            team.AdvancedWithScore = captain.AdvancedWithScore;

            retVal.Add(team);
        }

        return retVal;
    }

    public Task<string[]> GetUserTeamIds(string userId)
        => _cacheService.GetOrCreateAsync
        (
            GetUserTeamIdsCacheKey(userId),
            async entry =>
            {
                var userTeamIds = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.UserId == userId)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .ToArrayAsync();

                entry.Value = userTeamIds;
                return userTeamIds;
            }
        );

    public async Task<bool> IsAtGamespaceLimit(string teamId, Data.Game game, CancellationToken cancellationToken)
    {
        var gameId = await GetGameId(teamId, cancellationToken);
        var activeGameChallenges = await GetChallengesWithActiveGamespace(teamId, gameId, cancellationToken);
        return activeGameChallenges.Count() >= game.GetGamespaceLimit();
    }

    public async Task<bool> IsOnTeam(string teamId, string userId)
    {
        return (await GetUserTeamIds(userId)).Any(tId => tId == teamId);
    }

    public async Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken)
    {
        var teamPlayers = await _playerStore
            .List()
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var oldCaptain = teamPlayers.SingleOrDefault(p => p.Role == PlayerRole.Manager);
        var newCaptain = teamPlayers.Single(p => p.Id == newCaptainPlayerId);

        using (var transaction = await _playerStore.DbContext.Database.BeginTransactionAsync())
        {
            await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == teamId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, PlayerRole.Member));

            var affectedPlayers = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.Id == newCaptainPlayerId)
                .ExecuteUpdateAsync
                (
                    p => p.SetProperty(p => p.Role, PlayerRole.Manager)
                );

            // this automatically rolls back the transaction
            if (affectedPlayers != 1)
                throw new PromotionFailed(teamId, newCaptainPlayerId, affectedPlayers);

            await transaction.CommitAsync(cancellationToken);
        }

        await _teamHubService.SendPlayerRoleChanged(_mapper.Map<Api.Player>(newCaptain), actingUser);
    }

    public async Task<Data.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ToArrayAsync(cancellationToken);

        return ResolveCaptain(players);
    }

    public Data.Player ResolveCaptain(IEnumerable<Data.Player> players)
    {
        var teamIds = players.Select(p => p.TeamId).Distinct();
        if (teamIds.Count() != 1)
            throw new PlayersAreFromMultipleTeams(teamIds);

        var teamId = teamIds.First();
        if (!players.Any())
        {
            throw new CaptainResolutionFailure(teamId, "This team doesn't have any players.");
        }

        // if the team has a captain (manager), yay
        // if they have too many, boo (pick one by name which is stupid but stupid things happen sometimes)
        // if they don't have one, pick by name among all players
        var captains = players.Where(p => p.IsManager);

        if (captains.Count() == 1)
            return captains.Single();
        else if (captains.Count() > 1)
            return captains.OrderBy(c => c.ApprovedName).First();

        return players.OrderBy(p => p.ApprovedName).First();
    }

    public async Task<IDictionary<string, Data.Player>> ResolveCaptains(IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var distinctTeamIds = teamIds.Distinct().ToArray();
        var teamMap = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToList(), cancellationToken);

        var retVal = new Dictionary<string, Data.Player>();
        foreach (var teamId in distinctTeamIds)
        {
            Data.Player captain = null;

            if (teamMap.TryGetValue(teamId, out List<Data.Player> value))
                captain = ResolveCaptain(value);

            retVal.Add(teamId, captain);
        }

        return retVal;
    }

    public Task SetSessionWindow(string teamId, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken)
        => SetSessionWindow(new string[] { teamId }, sessionWindow, cancellationToken);

    public async Task SetSessionWindow(IEnumerable<string> teamIds, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken)
    {
        // update players
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.IsLateStart, sessionWindow.IsLateStart)
                    .SetProperty(p => p.SessionBegin, sessionWindow.Start)
                    .SetProperty(p => p.SessionEnd, sessionWindow.End)
                    .SetProperty(p => p.SessionMinutes, sessionWindow.LengthInMinutes),
                cancellationToken
            );

        // and their challenges
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => teamIds.Contains(c.TeamId))
            .Select(c => new { c.Id, c.GameEngineType, c.EndTime })
            .ToArrayAsync(cancellationToken);

        var challengeIds = challenges.Select(c => c.Id).ToArray();

        await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeIds.Contains(c.Id))
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(c => c.StartTime, c => sessionWindow.Start)
                    .SetProperty(c => c.EndTime, c => sessionWindow.End),
                cancellationToken
            );

        // and then their gamespaces (if the end time is changing)
        var pushGamespaceUpdateTasks = challenges
            .Where(c => c.EndTime != sessionWindow.End)
            .Select(c => _gameEngine.ExtendSession(c.Id, sessionWindow.End, c.GameEngineType))
            .ToArray();

        if (pushGamespaceUpdateTasks.Any())
            await Task.WhenAll(pushGamespaceUpdateTasks);
    }

    private async Task<TeamState> GetTeamState(string teamId, SimpleEntity actor, CancellationToken cancellationToken)
    {
        var captain = await ResolveCaptain(teamId, cancellationToken);

        return new TeamState
        {
            Id = teamId,
            ApprovedName = captain.ApprovedName,
            Name = captain.Name,
            NameStatus = captain.NameStatus,
            GameId = captain.GameId,
            SessionBegin = captain.SessionBegin.IsEmpty() ? null : captain.SessionBegin,
            SessionEnd = captain.SessionEnd.IsEmpty() ? null : captain.SessionEnd,
            Actor = actor
        };
    }

    // private async Task UpdateSessionEnd(string teamId, DateTimeOffset sessionEnd, CancellationToken cancellationToken)
    // {
    //     var currentSession = await GetSession(teamId, cancellationToken);
    //     currentSession.End = sessionEnd;

    //     await UpdateSession(teamId, currentSession, cancellationToken);
    // }

    private string GetUserTeamIdsCacheKey(string userId)
        => $"UserTeamIds:{userId}";
}
