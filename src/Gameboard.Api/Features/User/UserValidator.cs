// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Validators
{

    public class UserValidator : IModelValidator
    {
        private readonly IUserStore _store;

        public UserValidator(
            IUserStore store
        )
        {
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is Entity)
                return _validate(model as Entity);

            if (model is NewUser)
                return _validate(model as NewUser);

            if (model is ChangedUser)
                return _validate(model as ChangedUser);

            return Task.CompletedTask;

        }

        private async Task _validate(Entity model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<User>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(ChangedUser model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<User>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(NewUser model)
        {
            await Task.CompletedTask;
        }

        private async Task<bool> Exists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.Retrieve(id)) is Data.User
            ;
        }
    }
}
