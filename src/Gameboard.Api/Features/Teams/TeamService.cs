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

namespace Gameboard.Api.Features.Teams;

public interface ITeamService
{
    Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken);
    Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken);
    Task<Team> GetTeam(string id, CancellationToken cancellationToken);
    Task<Api.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken);
    Api.Player ResolveCaptain(string teamId, IEnumerable<Api.Player> players);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken);
    Task UpdateTeamSponsors(string teamId);
}

internal class TeamService : ITeamService
{
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IInternalHubBus _teamHubService;
    private readonly IStore _store;

    public TeamService(
        IMapper mapper,
        INowService now,
        IInternalHubBus teamHubService,
        IStore store)
    {
        _mapper = mapper;
        _now = now;
        _store = store;
        _teamHubService = teamHubService;
    }

    public async Task DeleteTeam(string teamId, SimpleEntity actingUser, CancellationToken cancellationToken)
    {
        var teamState = await GetTeamState(teamId, actingUser, cancellationToken);

        // delete player records
        await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ExecuteDeleteAsync(cancellationToken);

        // notify hub that the team is deleted /players left so the client can respond
        await _teamHubService.SendTeamDeleted(teamState, actingUser);
    }

    public async Task<int> GetSessionCount(string teamId, string gameId, CancellationToken cancellationToken)
    {
        var now = _now.Get();

        return await _store
            .ListAsNoTracking<Data.Player>()
            .CountAsync
            (
                p =>
                    p.GameId == gameId &&
                    p.Role == PlayerRole.Manager &&
                    now < p.SessionEnd,
                cancellationToken
            );
    }

    public async Task<Team> GetTeam(string id, CancellationToken cancellationToken)
    {
        var players = await ListTeam(id, cancellationToken);
        if (!players.Any())
            return null;

        var team = _mapper.Map<Team>(players.First(p => p.IsManager));

        team.Members = _mapper.Map<TeamMember[]>(
            players.Select(p => p.User)
        );

        team.TeamSponsors = string.Join("|", players.Select(p => p.Sponsor));

        return team;
    }

    public async Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser, CancellationToken cancellationToken)
    {
        var teamPlayers = await ListTeam(teamId, cancellationToken);
        var oldCaptain = teamPlayers.SingleOrDefault(p => p.Role == PlayerRole.Manager);
        var newCaptain = teamPlayers.Single(p => p.Id == newCaptainPlayerId);

        await _store.DoTransaction(async () =>
        {
            await _store
                .ListAsNoTracking<Data.Player>()
                .Where(p => p.TeamId == teamId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, p => PlayerRole.Member), cancellationToken);

            var affectedPlayers = await _store
                .ListAsNoTracking<Data.Player>()
                .Where(p => p.Id == newCaptainPlayerId)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.Role, p => PlayerRole.Manager)
                        .SetProperty(p => p.TeamSponsors, p => oldCaptain.TeamSponsors ?? p.TeamSponsors),
                    cancellationToken
                );

            // this automatically rolls back the transaction
            if (affectedPlayers != 1)
                throw new PromotionFailed(teamId, newCaptainPlayerId, affectedPlayers);

            await UpdateTeamSponsors(teamId);
        }, cancellationToken);

        await _teamHubService.SendPlayerRoleChanged(_mapper.Map<Api.Player>(newCaptain), actingUser);
    }

    public async Task<Api.Player> ResolveCaptain(string teamId, CancellationToken cancellationToken)
    {
        var players = await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return ResolveCaptain(teamId, _mapper.Map<IEnumerable<Api.Player>>(players));
    }

    public Api.Player ResolveCaptain(string teamId, IEnumerable<Api.Player> players)
    {
        if (!players.Any())
            throw new CaptainResolutionFailure(teamId, "This team doesn't have any players.");

        var groupedByTeam = players.GroupBy(p => p.TeamId).ToDictionary(g => g.Key, g => g.ToList());
        if (groupedByTeam.Keys.Count != 1)
            throw new PlayersAreFromMultipleTeams(groupedByTeam.Select(g => g.Key));

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

    public async Task UpdateTeamSponsors(string teamId)
    {
        var members = await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .Select(p => new
            {
                p.Id,
                p.Sponsor,
                p.IsManager
            })
            .ToArrayAsync();

        if (members.Length == 0)
            return;

        var sponsors = string.Join('|', members
            .Select(p => p.Sponsor)
            .Distinct()
            .ToArray()
        );

        var manager = members.FirstOrDefault(p => p.IsManager);

        await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.Id == manager.Id)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.TeamSponsors, sponsors));
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

    private async Task<IEnumerable<Data.Player>> ListTeam(string teamId, CancellationToken cancellationToken)
        =>
        (
            await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ToArrayAsync(cancellationToken)
        ).AsEnumerable();
}
