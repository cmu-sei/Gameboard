// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators
{
    public class PlayerValidator: IModelValidator
    {
        private readonly IPlayerStore _store;

        public PlayerValidator(
            IPlayerStore store
        )
        {
            _store = store;
        }

        public Task Validate(object model)
        {
            if (model is Entity)
                return _validate(model as Entity);

            if (model is PlayerDataFilter)
                return _validate(model as PlayerDataFilter);

            if (model is NewPlayer)
                return _validate(model as NewPlayer);

            if (model is ChangedPlayer)
                return _validate(model as ChangedPlayer);

            if (model is PlayerEnlistment)
                return _validate(model as PlayerEnlistment);

            if (model is SessionStartRequest)
                return _validate(model as SessionStartRequest);

            if (model is SessionChangeRequest)
                return _validate(model as SessionChangeRequest);

            if (model is TeamAdvancement)
                return _validate(model as TeamAdvancement);

            throw new System.NotImplementedException();
        }

        private async Task _validate(PlayerDataFilter model)
        {
            await Task.CompletedTask;
        }

        private async Task _validate(Entity model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task _validate(SessionStartRequest model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task _validate(SessionChangeRequest model)
        {
            await Task.CompletedTask;
        }

        private async Task _validate(NewPlayer model)
        {
            if (
                (await GameExists(model.GameId)).Equals(false) ||
                (await UserExists(model.UserId)).Equals(false)
            )
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task _validate(ChangedPlayer model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task _validate(PlayerEnlistment model)
        {
            if (model.Code.IsEmpty())
                throw new InvalidInvitationCode();

            if (model.PlayerId.NotEmpty() && (await Exists(model.PlayerId)).Equals(false))
                throw new ResourceNotFound();

            if (model.UserId.NotEmpty() && (await UserExists(model.UserId)).Equals(false))
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task _validate(TeamAdvancement model)
        {
            // if (model.TeamId.IsEmpty())
            //     throw new ResourceNotFound();

            if ((await GameExists(model.GameId)).Equals(false))
                throw new ResourceNotFound();

            if ((await GameExists(model.NextGameId)).Equals(false))
                throw new ResourceNotFound();

            await Task.CompletedTask;
        }

        private async Task<bool> Exists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.Retrieve(id)) is Data.Player
            ;
        }

        private async Task<bool> GameExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _store.DbContext.Games.FindAsync(id)) is Data.Game
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
