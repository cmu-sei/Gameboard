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
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Features.Teams;

public interface ITeamService
{
    Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken);
    Task EndSession(string teamId, User actor, CancellationToken cancellationToken);
    Task<Api.Player> ExtendSession(ExtendTeamSessionRequest request, CancellationToken cancellationToken);
    Task<IEnumerable<SimpleEntity>> GetChallengesWithActiveGamespace(string teamId, string gameId, CancellationToken cancellationToken);
    Task<bool> GetExists(string teamId);
    Task<string> GetGameId(string teamId, CancellationToken cancellationToken);
    Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken);
    Task<Team> GetTeam(string id);
    Task<IEnumerable<Team>> GetTeams(IEnumerable<string> ids);
    Task<bool> IsAtGamespaceLimit(string teamId, Data.Game game, CancellationToken cancellationToken);
    Task<bool> IsOnTeam(string teamId, string userId);
    Task<Data.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken);
    Data.Player ResolveCaptain(IEnumerable<Data.Player> players);
    Task<IDictionary<string, Data.Player>> ResolveCaptains(IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken);
    Task UpdateSessionStartAndEnd(string teamId, DateTimeOffset? sessionStart, DateTimeOffset? sessionEnd, CancellationToken cancellationToken);
}

internal class TeamService : ITeamService
{
    private readonly IGameEngineService _gameEngine;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memCache;
    private readonly INowService _now;
    private readonly IInternalHubBus _teamHubService;
    private readonly IPlayerStore _playerStore;
    private readonly IPracticeService _practiceService;
    private readonly IStore _store;

    public TeamService
    (
        IGameEngineService gameEngine,
        IMapper mapper,
        IMediator mediator,
        IMemoryCache memCache,
        INowService now,
        IInternalHubBus teamHubService,
        IPlayerStore playerStore,
        IPracticeService practiceService,
        IStore store
    )
    {
        _gameEngine = gameEngine;
        _mapper = mapper;
        _mediator = mediator;
        _memCache = memCache;
        _now = now;
        _playerStore = playerStore;
        _practiceService = practiceService;
        _store = store;
        _teamHubService = teamHubService;
    }

    public async Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken)
    {
        var teamState = await GetTeamState(teamId, actingUser, cancellationToken);

        // delete player records
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ExecuteDeleteAsync(cancellationToken);

        // also delete any external data for this team
        // TODO (fold hub call into this as well)
        await _mediator.Publish(new TeamDeletedNotification(teamId));

        // notify hub that the team is deleted /players left so the client can respond
        await _teamHubService.SendTeamDeleted(teamState, actingUser);
    }

    public Task EndSession(string teamId, User actor, CancellationToken cancellationToken)
        => UpdateSessionEnd(teamId, _now.Get(), cancellationToken);

    public async Task<Api.Player> ExtendSession(ExtendTeamSessionRequest request, CancellationToken cancellationToken)
    {
        // find the team and their captain
        var captain = await ResolveCaptain(request.TeamId, cancellationToken);

        // be sure they have an active session before we go extending things
        var playersWithNoSession = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .WhereDateIsEmpty(p => p.SessionBegin)
            .AnyAsync(cancellationToken);

        if (playersWithNoSession)
            throw new CantExtendUnstartedSession(request.TeamId);

        // in competitive mode, session end is what's requested in the API call
        var finalSessionEnd = request.NewSessionEnd;
        // in practice mode, there's special super secret logic (which is currently that the request results in 
        // a one-hour extension up to a cap defined in settings)
        if (captain.IsPractice)
            finalSessionEnd = await _practiceService.GetExtendedSessionEnd(captain.SessionBegin, captain.SessionEnd, cancellationToken);

        // update the player entities and gamespaces
        await UpdateSessionEnd(request.TeamId, finalSessionEnd, cancellationToken);

        // return the updated session via the captain
        // manually set the new session end here, because this object is stale
        captain.SessionEnd = finalSessionEnd;
        var captainModel = _mapper.Map<Api.Player>(captain);

        // update the notifications hub on the client side
        await _teamHubService.SendTeamUpdated(captainModel, request.Actor);
        await _teamHubService.SendTeamSessionExtended(new TeamState
        {
            Id = captain.TeamId,
            ApprovedName = captain.ApprovedName,
            Name = captain.Name,
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

    public async Task<bool> GetExists(string teamId)
        => await _playerStore.ListTeam(teamId).AnyAsync();

    public Task<string> GetGameId(string teamId, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .Select(p => p.GameId)
            .Distinct()
            .SingleAsync(cancellationToken);

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
            .Where(p => ids.Contains(p.TeamId))
            .GroupBy(p => p.TeamId, p => p)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray());

        if (teamPlayers.Count == 0)
            return Array.Empty<Team>();

        foreach (var teamId in teamPlayers.Keys)
        {
            var team = _mapper.Map<Team>(ResolveCaptain(teamPlayers[teamId]));
            team.Members = _mapper.Map<TeamMember[]>(teamPlayers[teamId]);
            team.Sponsors = _mapper.Map<Sponsor[]>(teamPlayers[teamId].Select(p => p.Sponsor));
            retVal.Add(team);
        }

        return retVal;
    }

    public async Task<bool> IsAtGamespaceLimit(string teamId, Data.Game game, CancellationToken cancellationToken)
    {
        var activeGameChallenges = await GetChallengesWithActiveGamespace(teamId, game.Id, cancellationToken);
        return activeGameChallenges.Count() >= game.GetGamespaceLimit();
    }

    public async Task<bool> IsOnTeam(string teamId, string userId)
    {
        // simple serialize to indicate whether this user and team are a match
        var cacheKey = $"{teamId}|{userId}";

        if (_memCache.TryGetValue(cacheKey, out bool cachedIsOnTeam))
            return cachedIsOnTeam;

        var teamUserIds = await _playerStore
            .ListTeam(teamId)
            .Select(p => p.UserId).ToArrayAsync();

        var isOnTeam = teamUserIds.Contains(userId);
        _memCache.Set(cacheKey, isOnTeam, TimeSpan.FromMinutes(30));

        return isOnTeam;
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
            await _playerStore
                .List()
                .Where(p => p.TeamId == teamId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, p => PlayerRole.Member));

            var affectedPlayers = await _playerStore
                .List()
                .Where(p => p.Id == newCaptainPlayerId)
                .ExecuteUpdateAsync
                (
                    p => p.SetProperty(p => p.Role, p => PlayerRole.Manager)
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
            return captains.First();
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

    private async Task<TeamState> GetTeamState(string teamId, SimpleEntity actor, CancellationToken cancellationToken)
    {
        var captain = await ResolveCaptain(teamId, cancellationToken);

        return new TeamState
        {
            Id = teamId,
            ApprovedName = captain.ApprovedName,
            Name = captain.Name,
            SessionBegin = captain.SessionBegin.IsEmpty() ? null : captain.SessionBegin,
            SessionEnd = captain.SessionEnd.IsEmpty() ? null : captain.SessionEnd,
            Actor = actor
        };
    }

    private Task UpdateSessionEnd(string teamId, DateTimeOffset sessionEnd, CancellationToken cancellationToken)
        => UpdateSessionStartAndEnd(teamId, null, sessionEnd, cancellationToken);

    public async Task UpdateSessionStartAndEnd(string teamId, DateTimeOffset? sessionStart, DateTimeOffset? sessionEnd, CancellationToken cancellationToken)
    {
        if (sessionStart is null && sessionEnd is null)
            throw new ArgumentException($"Either {nameof(sessionStart)} or {nameof(sessionEnd)} must be non-null.");

        // update all players
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.SessionBegin, p => sessionStart ?? p.SessionBegin)
                    .SetProperty(p => p.SessionEnd, p => sessionEnd ?? p.SessionEnd),
                cancellationToken
            );

        // and their challenges
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == teamId)
            .Select(c => new
            {
                c.Id,
                c.GameEngineType
            })
            .ToArrayAsync(cancellationToken);

        // NOTE ABOUT THE BELOW:
        //
        // This query structure looks preferable since it only involves a single
        // read/write. The issue we had was that during deployment of external +
        // sync-start games, bringing the challenges back with tracking
        // included a stale State property. (Not sure why, since we update
        // the State to the game engine's representation of the state during 
        // deployment). Since we were doing a full Update here, we ended up updating
        // the State property of the challenge even though we're not explicitly doing that
        // here, thus persisting the stale value. This may suggest a problem
        // in the Store or some weird EF stuff we're not taking into account.
        //
        // TL;DR - debugging external + sync start deploy is really challenging because of
        // external dependencies like Gamebrain, so we had to target our updates
        // to the desired properties here and move on.

        // var challenges = await _store
        //     .WithTracking<Data.Challenge>()
        //     .Where(c => c.TeamId == teamId)
        //     .ToArrayAsync(cancellationToken);

        // foreach (var challenge in challenges)
        // {
        //     if (sessionStart is not null)
        //         challenge.StartTime = sessionStart.Value;

        //     if (sessionEnd is not null)
        //         challenge.EndTime = sessionEnd.Value;
        // }
        //
        // await _store.SaveUpdateRange(challenges);

        // resolve these to IDs to allow query translation
        var challengeIds = challenges.Select(c => c.Id).ToArray();

        await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeIds.Contains(c.Id))
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(c => c.StartTime, c => sessionStart ?? c.StartTime)
                    .SetProperty(c => c.EndTime, c => sessionEnd ?? c.EndTime),
                cancellationToken
            );

        // and then their gamespaces (if the end time is changing)
        if (sessionEnd is not null)
        {
            var gamespaceUpdates = challenges.Select(c => _gameEngine.ExtendSession(c.Id, sessionEnd.Value, c.GameEngineType));
            await Task.WhenAll(gamespaceUpdates);

            // notify listeners of the session extension
            await _mediator.Publish(new TeamSessionExtendedNotification(teamId, sessionEnd.Value), cancellationToken);
        }
    }
}
