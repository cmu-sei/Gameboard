using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public interface ITeamService
{
    Task<bool> GetExists(string teamId);
    Task<int> GetSessionCount(string teamId, string gameId);
    Task<Team> GetTeam(string id);
    Task<Data.Player> ResolveCaptain(string teamId);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser);
    Task UpdateTeamSponsors(string teamId);
}

internal class TeamService : ITeamService
{
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IInternalHubBus _teamHubService;
    private readonly IPlayerStore _store;

    public TeamService(
        IMapper mapper,
        INowService now,
        IInternalHubBus teamHubService,
        IPlayerStore store)
    {
        _mapper = mapper;
        _now = now;
        _store = store;
        _teamHubService = teamHubService;
    }

    public async Task<bool> GetExists(string teamId)
    {
        return (await _store.ListTeam(teamId).CountAsync()) > 0;
    }

    public async Task<int> GetSessionCount(string teamId, string gameId)
    {
        var now = _now.Get();

        return await _store
            .List()
            .CountAsync
            (
                p =>
                    p.GameId == gameId &&
                    p.Role == PlayerRole.Manager &&
                    now < p.SessionEnd
            );
    }

    public async Task<Team> GetTeam(string id)
    {
        var players = await _store.ListTeam(id).ToArrayAsync();
        if (players.Count() == 0)
            return null;

        var team = _mapper.Map<Team>(
            players.First(p => p.IsManager)
        );

        team.Members = _mapper.Map<TeamMember[]>(
            players.Select(p => p.User)
        );

        team.TeamSponsors = string.Join("|", players.Select(p => p.Sponsor));

        return team;
    }

    public async Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser)
    {
        var teamPlayers = await _store
            .List()
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .ToListAsync();

        var oldCaptain = teamPlayers.SingleOrDefault(p => p.Role == PlayerRole.Manager);
        var newCaptain = teamPlayers.Single(p => p.Id == newCaptainPlayerId);

        using (var transaction = await _store.DbContext.Database.BeginTransactionAsync())
        {
            await _store
                .List()
                .Where(p => p.TeamId == teamId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, p => PlayerRole.Member));

            var affectedPlayers = await _store
                .List()
                .Where(p => p.Id == newCaptainPlayerId)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.Role, p => PlayerRole.Manager)
                        .SetProperty(p => p.TeamSponsors, p => oldCaptain.TeamSponsors ?? p.TeamSponsors)
                );

            // this automatically rolls back the transaction
            if (affectedPlayers != 1)
                throw new PromotionFailed(teamId, newCaptainPlayerId, affectedPlayers);

            await UpdateTeamSponsors(teamId);

            await transaction.CommitAsync();
        }

        await _teamHubService.SendPlayerRoleChanged(_mapper.Map<Api.Player>(newCaptain), actingUser);
    }

    public async Task<Data.Player> ResolveCaptain(string teamId)
    {
        var players = await _store
            .List()
            .Where(p => p.TeamId == teamId)
            .ToListAsync();

        if (players.Count() == 0)
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

    public async Task UpdateTeamSponsors(string teamId)
    {
        var members = await _store
            .List()
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .Select(p => new
            {
                Id = p.Id,
                Sponsor = p.Sponsor,
                IsManager = p.IsManager
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
            .List()
            .Where(p => p.Id == manager.Id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(p => p.TeamSponsors, sponsors));
    }
}
