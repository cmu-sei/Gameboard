// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data
{
    public class FeedbackStore : Store<Feedback>, IFeedbackStore
    {
        public FeedbackStore(GameboardDbContext dbContext, IGuidService guids)
            : base(dbContext, guids) { }

        public async Task<Feedback> Load(string id)
        {
            return await DbSet.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Feedback> Load(Feedback model)
        {
            return await DbSet
                .FirstOrDefaultAsync(s =>
                    s.ChallengeSpecId == model.ChallengeSpecId &&
                    s.ChallengeId == model.ChallengeId &&
                    s.UserId == model.UserId &&
                    s.GameId == model.GameId
                );
            ;
        }
    }
}
