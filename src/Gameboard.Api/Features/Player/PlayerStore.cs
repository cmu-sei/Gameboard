// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public interface IPlayerStore : IStore<Player>
{
    Task DeleteTeam(string teamId);
    Task<User> GetUserEnrollments(string id);
    IQueryable<Player> ListTeam(string id);
    Task<Player[]> ListTeamByPlayer(string id);
    Task<Challenge[]> ListTeamChallenges(string id);
    Task<Player> LoadBoard(string id);
}

public class PlayerStore : Store<Data.Player>, IPlayerStore
{
    public PlayerStore(IGuidService guids, GameboardDbContext dbContext)
        : base(dbContext, guids) { }

    public IQueryable<Player> ListTeam(string id) =>
        base.List()
            .Include(player => player.Sponsor)
            .Include(player => player.User)
            .Include(player => player.Game)
            .Where(p => p.TeamId == id);

    public async Task DeleteTeam(string teamId) => await DbContext
        .Players
        .Where(p => p.TeamId == teamId)
        .ExecuteDeleteAsync();

    public async Task<Player[]> ListTeamByPlayer(string id)
    {
        var player = await base.Retrieve(id);

        return await base.List()
            .Where(p => p.TeamId == player.TeamId)
            .ToArrayAsync();
    }

    public async Task<Challenge[]> ListTeamChallenges(string id)
        => await DbContext.Challenges
            .AsNoTracking()
            .Include(c => c.Events)
            .Where(c => c.TeamId == id)
            .ToArrayAsync();

    public async Task<User> GetUserEnrollments(string id)
        => await DbContext.Users
            .Include(u => u.Enrollments)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Player> LoadBoard(string id)
    {
        var result = await DbSet.AsNoTracking()
            .Include(p => p.Game)
                .ThenInclude(g => g.Specs)
            .Include(p => p.Game)
                .ThenInclude(g => g.Prerequisites)
            .Include(p => p.Challenges)
                .ThenInclude(c => c.Events)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (result.Game.AllowTeam)
            result.Challenges = await DbContext
                .Challenges
                .AsNoTracking()
                .Include(c => c.Events)
                .Where(c => c.TeamId == result.TeamId)
                .ToArrayAsync();

        return result;
    }
}
