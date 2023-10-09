// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;

namespace Gameboard.Api.Validators
{
    public class ChallengeValidator : IModelValidator
    {
        private readonly IStore<Data.ChallengeSpec> _specStore;
        private readonly IChallengeStore _store;
        private readonly IPlayerStore _playerStore;

        public ChallengeValidator(IChallengeStore store, IStore<Data.ChallengeSpec> specStore, IPlayerStore playerStore)
        {
            _playerStore = playerStore;
            _specStore = specStore;
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is Entity)
                return _validate(model as Entity);

            if (model is NewChallenge)
                return _validate(model as NewChallenge);

            if (model is ChangedChallenge)
                return _validate(model as ChangedChallenge);

            throw new System.NotImplementedException();
        }

        private async Task _validate(Entity model)
        {
            if ((await _store.Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Challenge>(model.Id);
        }

        private async Task _validate(NewChallenge model)
        {
            if ((await _playerStore.Exists(model.PlayerId)).Equals(false))
                throw new ResourceNotFound<Data.Player>(model.PlayerId);

            if ((await _specStore.Exists(model.SpecId)).Equals(false))
                throw new ResourceNotFound<Data.ChallengeSpec>(model.SpecId);

            var player = await _store.DbContext.Players.FindAsync(model.PlayerId);

            if (!player.IsPractice && player.IsLive.Equals(false))
                throw new SessionNotActive(player.Id);

            var spec = await _store.DbContext.ChallengeSpecs.FindAsync(model.SpecId);

            if (spec.GameId != player.GameId)
                throw new ActionForbidden();

            // Note: not checking "already exists" since this is used idempotently
            await Task.CompletedTask;
        }

        private async Task _validate(ChangedChallenge model)
        {
            if ((await _store.Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Data.Challenge>(model.Id);

            await Task.CompletedTask;
        }
    }
}
