// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Validators
{
    public class SponsorValidator : IModelValidator
    {
        private readonly IStore<Data.Sponsor> _store;

        public SponsorValidator(IStore<Data.Sponsor> store)
        {
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is Entity)
                return _validate(model as Entity);

            if (model is NewSponsor)
                return _validate(model as NewSponsor);

            if (model is ChangedSponsor)
                return _validate(model as ChangedSponsor);

            throw new System.NotImplementedException();
        }

        private async Task _validate(Entity model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Sponsor>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(ChangedSponsor model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Sponsor>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(NewSponsor model)
        {
            if ((await Exists(model.Id)).Equals(true))
                throw new AlreadyExists();

            await Task.CompletedTask;
        }

        private async Task<bool> Exists(string id)
        {
            return id.NotEmpty() && await _store.Exists(id);
        }

    }
}
