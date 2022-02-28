// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators
{
    public class FeedbackValidator: IModelValidator
    {
        private readonly IFeedbackStore _store;

        public FeedbackValidator(
            IFeedbackStore store
        )
        {
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is FeedbackSubmission)
                return _validate(model as FeedbackSubmission);

            throw new System.NotImplementedException();
        }

        private async Task _validate(FeedbackSubmission model)
        {
            if (model.ChallengeSpecId.NotEmpty() && (await SpecExists(model.ChallengeSpecId)).Equals(false))
                throw new ResourceNotFound();

            if (model.ChallengeId.NotEmpty() && (await ChallengeExists(model.ChallengeId)).Equals(false))
                throw new ResourceNotFound();

            if (model.ChallengeId.IsEmpty() != model.ChallengeSpecId.IsEmpty())
                throw new InvalideFeedbackFormat();

            if ((await GameExists(model.GameId)).Equals(false))
                throw new ResourceNotFound();

            if (model.ChallengeSpecId.NotEmpty() || model.ChallengeId.NotEmpty()) { // aka, a challenge-specific feedback response
                var game = await _store.DbContext.Games.FindAsync(model.GameId);
                var spec = await _store.DbContext.ChallengeSpecs.FindAsync(model.ChallengeSpecId);
                var challenge = await _store.DbContext.Challenges.FindAsync(model.ChallengeId);

                if (spec.GameId != game.Id)
                throw new ActionForbidden();

                if (spec.Id != challenge.SpecId)
                throw new ActionForbidden();
            }
            
            await Task.CompletedTask;
        }

        private async Task<bool> GameExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Games.FindAsync(id)) is Data.Game
            ;
        }

        private async Task<bool> SpecExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.ChallengeSpecs.FindAsync(id)) is Data.ChallengeSpec
            ;
        }

        private async Task<bool> ChallengeExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Challenges.FindAsync(id)) is Data.Challenge
            ;
        }

        private async Task<bool> PlayerExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Players.FindAsync(id)) is Data.Player
            ;
        }

        private async Task<bool> UserExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Users.FindAsync(id)) is Data.User
            ;
        }
    }
}
