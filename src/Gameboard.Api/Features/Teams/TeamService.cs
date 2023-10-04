using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Features.Teams;

public interface ITeamService
{
    Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken);
    Task<IEnumerable<SimpleEntity>> GetChallengesWithActiveGamespace(string teamId, string gameId, CancellationToken cancellationToken);
    Task<bool> GetExists(string teamId);
    Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken);
    Task<Team> GetTeam(string id);
    Task<bool> IsAtGamespaceLimit(string teamId, Data.Game game, CancellationToken cancellationToken);
    Task<bool> IsOnTeam(string teamId, string userId);
    Task<Data.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken);
    Data.Player ResolveCaptain(IEnumerable<Data.Player> players);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken);
}

internal class TeamService : ITeamService
{
    private readonly IMapper _mapper;
    private readonly IMemoryCache _memCache;
    private readonly INowService _now;
    private readonly IInternalHubBus _teamHubService;
    private readonly IPlayerStore _playerStore;
    private readonly IStore _store;

    public TeamService
    (
        IMapper mapper,
        IMemoryCache memCache,
        INowService now,
        IInternalHubBus teamHubService,
        IPlayerStore playerStore,
        IStore store
    )
    {
        _mapper = mapper;
        _memCache = memCache;
        _now = now;
        _playerStore = playerStore;
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

        // notify hub that the team is deleted /players left so the client can respond
        await _teamHubService.SendTeamDeleted(teamState, actingUser);
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
        var players = await _playerStore.ListTeam(id).ToArrayAsync();
        if (players.Length == 0)
            return null;

        var team = _mapper.Map<Team>(players.First(p => p.IsManager));

        team.Members = _mapper.Map<TeamMember[]>(players.Select(p => p.User));
        team.Sponsors = _mapper.Map<Sponsor[]>(players.Select(p => p.Sponsor));

        return team;
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

            await transaction.CommitAsync();
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
        {
            return captains.First();
        }
        else if (captains.Count() > 1)
        {
            return captains.OrderBy(c => c.ApprovedName).First();
        }

        return players.OrderBy(p => p.ApprovedName).First();
    }

    private async Task<TeamState> GetTeamState(string teamId, SimpleEntity actor, CancellationToken cancellationToken)
    {
        var captain = await ResolveCaptain(teamId, cancellationToken);

        return new TeamState
        {
            Id = teamId,
            Name = captain.Name,
            SessionBegin = captain.SessionBegin.IsEmpty() ? null : captain.SessionBegin,
            SessionEnd = captain.SessionEnd.IsEmpty() ? null : captain.SessionEnd,
            Actor = actor
        };
    }
}
