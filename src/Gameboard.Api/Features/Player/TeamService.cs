using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Player;

public interface ITeamService
{
    Task<Data.Player> ResolveCaptain(string teamId);
    Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser);
}

internal class TeamService : ITeamService
{
    private readonly IMapper _mapper;
    private readonly IInternalHubBus _teamHubService;
    private readonly IPlayerStore _store;

    public TeamService(
        IMapper mapper,
        IInternalHubBus teamHubService,
        IPlayerStore store)
    {
        _mapper = mapper;
        _store = store;
        _teamHubService = teamHubService;
    }

    public async Task PromoteCaptain(string teamId, string newCaptainPlayerId, User actingUser)
    {
        var teamPlayers = await _store
            .List()
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .ToListAsync();

        var newCaptain = teamPlayers.Single(p => p.Id == newCaptainPlayerId);

        using (var transaction = await _store.DbContext.Database.BeginTransactionAsync())
        {
            await _store.DbContext
                .Players
                .Where(p => p.TeamId == teamId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, p => PlayerRole.Member));

            await _store.DbContext
                .Players
                .Where(p => p.Id == newCaptainPlayerId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.Role, p => PlayerRole.Manager));

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
}
