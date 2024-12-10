// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api.Validators
{
    public class FeedbackValidator(IStore store) : IModelValidator
    {
        private readonly IStore _store = store;

        public Task Validate(object model)
        {
            if (model is Features.Feedback.FeedbackSubmissionLegacy)
                return _validate(model as Features.Feedback.FeedbackSubmissionLegacy);

            throw new System.NotImplementedException();
        }

        private async Task _validate(Features.Feedback.FeedbackSubmissionLegacy model)
        {
            if (!await _store.AnyAsync<Data.Game>(g => g.Id == model.GameId, CancellationToken.None))
                throw new ResourceNotFound<Data.Game>(model.GameId);

            if (model.ChallengeId.IsEmpty() != model.ChallengeSpecId.IsEmpty()) // must specify both or neither
                throw new InvalidFeedbackFormat();

            if (model.ChallengeId.IsNotEmpty() && !await _store.AnyAsync<Data.Challenge>(c => c.Id == model.ChallengeId, CancellationToken.None))
            {
                throw new ResourceNotFound<Challenge>(model.ChallengeSpecId);
            }

            if (model.ChallengeSpecId.IsNotEmpty())
            {
                if (!await _store.AnyAsync<Data.ChallengeSpec>(s => s.Id == model.ChallengeSpecId, CancellationToken.None))
                    throw new ResourceNotFound<ChallengeSpec>(model.ChallengeSpecId);

                // if specified, this is a challenge-specific feedback response, so validate challenge/spec/game match
                if (!await _store.AnyAsync<Data.ChallengeSpec>(s => s.Id == model.ChallengeSpecId && s.GameId == model.GameId, CancellationToken.None))
                    throw new ActionForbidden();

                if (!await _store.AnyAsync<Data.Challenge>(c => c.Id == model.ChallengeId && c.SpecId == model.ChallengeSpecId, CancellationToken.None))
                    throw new ActionForbidden();
            }
        }
    }
}
