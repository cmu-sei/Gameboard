// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Validators
{
    public class ChallengeSpecValidator : IModelValidator
    {
        private readonly IGameStore _gameStore;
        private readonly IStore<Data.ChallengeSpec> _specStore;

        public ChallengeSpecValidator(IStore<Data.ChallengeSpec> specStore, IGameStore gameStore)
        {
            _gameStore = gameStore;
            _specStore = specStore;
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
            if ((await _specStore.Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<ChallengeSpec>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(NewChallengeSpec model)
        {
            if ((await _gameStore.Exists(model.GameId)).Equals(false))
                throw new ResourceNotFound<Game>(model.GameId);

            await Task.CompletedTask;
        }
        private async Task _validate(ChangedChallengeSpec model)
        {
            if ((await _specStore.Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<ChallengeSpec>(model.Id);

            await Task.CompletedTask;
        }
    }
}
