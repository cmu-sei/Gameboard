// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data
{

    public class FeedbackStore: Store<Feedback>, IFeedbackStore
    {
        public FeedbackStore(GameboardDbContext dbContext)
        :base(dbContext)
        {

        }

        public async Task<Feedback> Load(string id)
        {
            return await DbSet
                .FirstOrDefaultAsync(c => c.Id == id)
            ;
        }

        public async Task<Feedback> Load(Feedback model)
        {
            return await DbSet
                .FirstOrDefaultAsync(c => c.Id == model.Id)
            ;
        }


        public async Task<Feedback> ResolveApiKey(string hash)
        {
            return await DbSet.FirstOrDefaultAsync();
        }
    }
}
