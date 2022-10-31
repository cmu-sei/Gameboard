// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data
{

    public class PlayerStore : Store<Player>, IPlayerStore
    {
        public PlayerStore(GameboardDbContext dbContext)
        : base(dbContext) { }

        public async Task<Player> Load(string id)
        {
            return await DbSet
                .AsNoTracking()
                // .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id)
            ;
        }

        public async Task<Player[]> ListTeam(string id)
        {
            // things are exploding
            var players = await this.DbContext.Players.ToListAsync();
            return players.Where(p => p.TeamId == id).ToArray();
        }

        public async Task<Player[]> ListTeamByPlayer(string id)
        {
            var player = await base.Retrieve(id);

            return await base.List()
                .Where(p => p.TeamId == player.TeamId)
                .ToArrayAsync()
            ;
        }

        public async Task<Challenge[]> ListTeamChallenges(string id)
        {
            var challengeEvents = await DbContext.Challenges
                .AsNoTracking()
                .Include(c => c.Events)
                .ToListAsync();

            return challengeEvents.Where(c => c.TeamId == id).ToArray();
        }

        public async Task<IEnumerable<Player>> GetExistingGames(string userId)
        {
            return await DbContext.Players
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        // public async Task<User> GetUserEnrollments(string id)
        // {
        //     // have to fetch these backward because ef core is being terrible
        //     // https://github.com/dotnet/efcore/issues/11564
        //     var players = await base.DbContext
        //         .Players
        //         .AsNoTracking()
        //         .Where(p => p.UserId == id)
        //         .ToListAsync();

        //     if (players.Count() == 0)
        //     {
        //         return await base.DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        //     }

        //     return players.First().User;
        // }

        public async Task<Player> LoadBoard(string id)
        {
            var result = await DbSet.AsNoTracking()
                .Include(p => p.Game)
                    .ThenInclude(g => g.Specs)
                .Include(p => p.Game)
                    .ThenInclude(g => g.Prerequisites)
                .Include(p => p.Challenges).ThenInclude(c => c.Events)
                .FirstOrDefaultAsync(p => p.Id == id)
            ;

            if (result.Game.AllowTeam)
                result.Challenges = await DbContext.Challenges.AsNoTracking()
                    .Include(c => c.Events)
                    .Where(c => c.TeamId == result.TeamId)
                    .ToArrayAsync();

            return result;
        }
    }
}
