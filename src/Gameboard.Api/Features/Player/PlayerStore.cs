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
    IQueryable<Player> ListTeam(string id);
    Task<Player[]> ListTeamByPlayer(string id);
    Task<Player> LoadBoard(string id);
}

public class PlayerStore(IGuidService guids, IDbContextFactory<GameboardDbContext> dbContextFactory, IStore store)
    : Store<Data.Player>(dbContextFactory, guids), IPlayerStore
{
    private readonly IStore _store = store;

    public IQueryable<Player> ListTeam(string id) =>
        base.List()
            .Include(player => player.Sponsor)
            .Include(player => player.User)
            .Include(player => player.Game)
            .Where(p => p.TeamId == id);

    public async Task<Player[]> ListTeamByPlayer(string id)
    {
        var player = await base.Retrieve(id);

        return await base.List()
            .Where(p => p.TeamId == player.TeamId)
            .ToArrayAsync();
    }

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
        {
            result.Challenges = await _store
                .WithNoTracking<Data.Challenge>()
                .Include(c => c.Events)
                .Where(c => c.TeamId == result.TeamId)
                .ToArrayAsync();
        }

        return result;
    }
}
