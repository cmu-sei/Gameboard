// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators
{
    public class ChallengeSpecValidator : IModelValidator
    {
        private readonly IChallengeStore _store;

        public ChallengeSpecValidator(IChallengeStore store)
        {
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is Entity)
                return _validate(model as Entity);

            if (model is NewChallengeSpec)
                return _validate(model as NewChallengeSpec);

            if (model is ChangedChallengeSpec)
                return _validate(model as ChangedChallengeSpec);

            throw new ValidationTypeFailure<ChallengeSpecValidator>(model.GetType());
        }

        private async Task _validate(Entity model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<ChallengeSpec>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(NewChallengeSpec model)
        {
            if ((await GameExists(model.GameId)).Equals(false))
                throw new ResourceNotFound<Game>(model.GameId);

            await Task.CompletedTask;
        }
        private async Task _validate(ChangedChallengeSpec model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<ChallengeSpec>(model.Id);

            await Task.CompletedTask;
        }

        private async Task<bool> Exists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.Retrieve(id)) is Data.Challenge
            ;
        }

        private async Task<bool> GameExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Games.FindAsync(id)) is Data.Game
            ;
        }

    }
}
