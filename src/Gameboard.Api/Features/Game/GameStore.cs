// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using System;

namespace Gameboard.Api.Data
{

    public class GameStore: Store<Game>, IGameStore
    {
        public GameStore(GameboardDbContext dbContext)
        :base(dbContext)
        {

        }

        public async Task<Game> Load(string id)
        {
            return await DbSet
                .Include(g => g.Specs)
                .FirstOrDefaultAsync(g => g.Id == id)
            ;
        }

        public override IQueryable<Game> List(string term = null)
        {
            var q = base.List();

            if (!string.IsNullOrEmpty(term))
            {
                term = term.ToLower();

                q = q.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    t.Season.ToLower().Contains(term) ||
                    t.Track.ToLower().Contains(term) ||
                    t.Division.ToLower().Contains(term) ||
                    t.Competition.ToLower().Contains(term) ||
                    t.Sponsor.ToLower().Contains(term) ||
                    t.Key.ToLower().Contains(term) ||
                    t.Mode.ToLower().Contains(term) ||
                    t.Id.ToLower().StartsWith(term)
                );
            }

            return q;
        }

    }
}
