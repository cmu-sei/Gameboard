using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Player;

public interface ITeamService
{
    Task<Data.Player> ResolveCaptain(string teamId);
}

internal class TeamService : ITeamService
{
    private readonly IPlayerStore _store;

    public TeamService(IPlayerStore store)
    {
        _store = store;
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