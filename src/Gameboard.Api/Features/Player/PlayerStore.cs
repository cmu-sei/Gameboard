// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data
{

    public class PlayerStore: Store<Player>, IPlayerStore
    {
        public PlayerStore(GameboardDbContext dbContext)
        :base(dbContext)
        {

        }

        public async Task<Player> Load(string id)
        {
            return await DbSet
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id)
            ;
        }

        public async Task<Player[]> ListTeam(string id)
        {
            return await base.List()
                .Include(p => p.User)
                .Include(p => p.Game)
                .Where(p => p.TeamId == id)
                .ToArrayAsync()
            ;
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
            return await DbContext.Challenges
                .Include(c => c.Events)
                .Where(c => c.TeamId == id)
                .ToArrayAsync()
            ;
        }

        public async Task<User> GetUserEnrollments(string id)
        {
            return await DbContext.Users
                .Include(u => u.Enrollments)
                .FirstOrDefaultAsync(u => u.Id == id)
            ;
        }

        public async Task<Player> LoadBoard(string id)
        {
            var result = await DbSet.AsNoTracking()
                .Include(p => p.Game).ThenInclude(g => g.Specs)
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

        // If entity has searchable fields, use this:
        // public override IQueryable<Player> List(string term = null)
        // {
        //     var q = base.List();

        //     if (!string.IsNullOrEmpty(term))
        //     {
        //         term = term.ToLower();

        //         q = q.Where(t =>
        //             t.Name.ToLower().Contains(term)
        //         );
        //     }

        //     return q;
        // }

    }
}
