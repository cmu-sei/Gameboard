// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using System.Threading;

namespace Gameboard.Api.Validators
{
    public class PlayerValidator : IModelValidator
    {
        private readonly IGameModeServiceFactory _gameModeServiceFactory;
        private readonly IPlayerStore _playerStore;
        private readonly IStore _store;

        public PlayerValidator
        (
            IGameModeServiceFactory gameModeServiceFactory,
            IPlayerStore playerStore,
            IStore store
        )
        {
            _gameModeServiceFactory = gameModeServiceFactory;
            _playerStore = playerStore;
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

            if (model is PlayerUnenrollRequest)
                return _validate(model as PlayerUnenrollRequest);

            if (model is PromoteToManagerRequest)
                return _validate(model as PromoteToManagerRequest);

            if (model is SessionStartRequest)
                return _validate(model as SessionStartRequest);

            if (model is TeamAdvancement)
                return _validate(model as TeamAdvancement);

            throw new ValidationTypeFailure<PlayerValidator>(model.GetType());
        }

        private Task _validate(PlayerDataFilter model)
            => Task.CompletedTask;

        private async Task _validate(Entity model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Data.Player>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(SessionStartRequest model)
        {
            if ((await Exists(model.PlayerId)).Equals(false))
                throw new ResourceNotFound<Player>(model.PlayerId);

            var player = await _playerStore.Retrieve(model.PlayerId);

            if (player.SessionBegin.Year > 1)
                throw new SessionAlreadyStarted(model.PlayerId, $"Player {model.PlayerId}'s session has already started.");

            await Task.CompletedTask;
        }

        private async Task _validate(NewPlayer model)
        {
            if (!await GameExists(model.GameId))
                throw new ResourceNotFound<Data.Game>(model.GameId);

            if (!await UserExists(model.UserId))
                throw new ResourceNotFound<User>(model.UserId);

            // if the game is sync start and has started, don't allow registration
            var game = await _store
                .WithNoTracking<Data.Game>()
                .SingleAsync(g => g.Id == model.GameId);

            if (game.RequireSynchronizedStart && game.Mode == GameEngineMode.External)
            {
                var gameModeService = await _gameModeServiceFactory.Get(model.GameId);
                var state = await gameModeService.GetGamePlayState(model.GameId, CancellationToken.None);

                if (state != GamePlayState.NotRegistered && state != GamePlayState.NotStarted)
                    throw new CantEnrollWithIneligibleGamePlayState(model.UserId, model.GameId, state, GamePlayState.NotRegistered, GamePlayState.NotRegistered);
            }

            await Task.CompletedTask;
        }

        private async Task _validate(ChangedPlayer model)
        {
            if ((await Exists(model.Id)).Equals(false))
                throw new ResourceNotFound<Player>(model.Id);

            await Task.CompletedTask;
        }

        private async Task _validate(PlayerEnlistment model)
        {
            if (model.Code.IsEmpty())
                throw new InvalidInvitationCode(model.Code, "No code was provided.");

            if (model.PlayerId.NotEmpty() && (await Exists(model.PlayerId)).Equals(false))
                throw new ResourceNotFound<Player>(model.PlayerId);

            if (model.UserId.NotEmpty() && (await UserExists(model.UserId)).Equals(false))
                throw new ResourceNotFound<User>(model.UserId);

            await Task.CompletedTask;
        }

        private async Task _validate(PromoteToManagerRequest model)
        {
            // INDEPENDENT OF ADMIN
            var currentManager = await _playerStore.List().SingleOrDefaultAsync(p => p.Id == model.CurrentManagerPlayerId);

            if (currentManager == null)
                throw new ResourceNotFound<Player>(model.CurrentManagerPlayerId, $"Couldn't resolve the player record for current manager {model.CurrentManagerPlayerId}.");

            if (!currentManager.IsManager)
                throw new PlayerIsntManager(model.CurrentManagerPlayerId, "Calls to this endpoint must supply the correct ID of the current manager.");

            var newManager = await _playerStore.List().SingleOrDefaultAsync(p => p.Id == model.NewManagerPlayerId);
            if (newManager == null)
                throw new ResourceNotFound<Player>(model.NewManagerPlayerId, $"Couldn't resolve the player record for new manager {model.NewManagerPlayerId}");

            if (currentManager.TeamId != newManager.TeamId)
                throw new NotOnSameTeam(currentManager.Id, currentManager.TeamId, newManager.Id, newManager.TeamId, "Players must be on the same team to promote a new manager.");

            if (IsActingAsAdmin(model.Actor))
                return;
        }

        private async Task _validate(TeamAdvancement model)
        {
            if ((await GameExists(model.GameId)).Equals(false))
                throw new ResourceNotFound<Data.Game>(model.GameId);

            if ((await GameExists(model.NextGameId)).Equals(false))
                throw new ResourceNotFound<Data.Game>(model.NextGameId, "The next game");

            await Task.CompletedTask;
        }

        public async Task _validate(PlayerUnenrollRequest request)
        {
            var player = await _store
                .WithNoTracking<Data.Player>()
                .SingleOrDefaultAsync(p => p.Id == request.PlayerId);

            if (player is null)
                throw new ResourceNotFound<Player>(request.PlayerId);

            if (!IsActingAsAdmin(request.Actor) && player.SessionBegin > DateTimeOffset.MinValue)
                throw new SessionAlreadyStarted(request.PlayerId, "Non-admins can't unenroll from a game once they've started a session.");

            // this is order-sensitive - non-managers can unenroll as long as the session isn't started
            if (!player.IsManager)
                return;

            var teammateIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p =>
                    p.TeamId == player.TeamId &&
                    p.Id != request.PlayerId
                )
                .Select(p => p.Id)
                .ToArrayAsync();

            if (!IsActingAsAdmin(request.Actor) && teammateIds.Any())
                throw new ManagerCantUnenrollWhileTeammatesRemain(player.Id, player.TeamId, teammateIds);
        }

        private bool IsActingAsAdmin(User actor)
            => actor.IsAdmin || actor.IsRegistrar;

        private async Task<bool> Exists(string id)
        {
            return
                id.NotEmpty() && await _playerStore
                    .DbContext
                    .Players
                    .Where(p => p.Id == id)
                    .AnyAsync();
        }

        private async Task<bool> GameExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _playerStore.DbContext.Games.FindAsync(id)) is Data.Game
            ;
        }

        private async Task<bool> UserExists(string id)
        {
            return
                id.NotEmpty() &&
                (await _playerStore.DbContext.Users.FindAsync(id)) is Data.User
            ;
        }
    }
}
