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
using Gameboard.Api.Features.Users;

namespace Gameboard.Api.Validators;

public class PlayerValidator(
    IGameModeServiceFactory gameModeServiceFactory,
    IUserRolePermissionsService permissionsService,
    IStore store
    ) : IModelValidator
{
    private readonly IGameModeServiceFactory _gameModeServiceFactory = gameModeServiceFactory;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;

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
        if (!await _store.Exists<Data.Player>(model.Id))
            throw new ResourceNotFound<Data.Player>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(SessionStartRequest model)
    {
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Select(p => new { p.Id, p.SessionBegin })
            .Where(p => p.Id == model.PlayerId)
            .SingleOrDefaultAsync() ?? throw new ResourceNotFound<Player>(model.PlayerId);

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

            if (state != GamePlayState.NotStarted)
                throw new CantEnrollWithIneligibleGamePlayState(model.UserId, model.GameId, state, GamePlayState.NotStarted);
        }

        await Task.CompletedTask;
    }

    private async Task _validate(ChangedPlayer model)
    {
        if (!await _store.Exists<Data.Player>(model.Id))
            throw new ResourceNotFound<Player>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(PlayerEnlistment model)
    {
        if (model.Code.IsEmpty())
            throw new InvalidInvitationCode(model.Code, "No code was provided.");

        if (model.PlayerId.NotEmpty() && (!await _store.Exists<Data.Player>(model.PlayerId)))
            throw new ResourceNotFound<Player>(model.PlayerId);

        if (model.UserId.NotEmpty() && (await UserExists(model.UserId)).Equals(false))
            throw new ResourceNotFound<User>(model.UserId);

        await Task.CompletedTask;
    }

    private async Task _validate(PromoteToManagerRequest model)
    {
        // INDEPENDENT OF ADMIN
        var currentManager = await _store
            .WithNoTracking<Data.Player>()
            .SingleOrDefaultAsync(p => p.Id == model.CurrentCaptainId)
            ?? throw new ResourceNotFound<Player>(model.CurrentCaptainId, $"Couldn't resolve the player record for current manager {model.CurrentCaptainId}.");

        if (!currentManager.IsManager)
            throw new PlayerIsntManager(model.CurrentCaptainId, "Calls to this endpoint must supply the correct ID of the current manager.");

        var newManager = await _store
            .WithNoTracking<Data.Player>()
            .SingleOrDefaultAsync(p => p.Id == model.NewManagerPlayerId)
            ?? throw new ResourceNotFound<Player>(model.NewManagerPlayerId, $"Couldn't resolve the player record for new manager {model.NewManagerPlayerId}");

        if (currentManager.TeamId != newManager.TeamId)
            throw new NotOnSameTeam(currentManager.Id, currentManager.TeamId, newManager.Id, newManager.TeamId, "Players must be on the same team to promote a new manager.");
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
            .Select(p => new
            {
                p.Id,
                p.Role,
                HasStartedSession = p.SessionBegin > DateTimeOffset.MinValue,
                p.TeamId,
            })
            .SingleOrDefaultAsync(p => p.Id == request.PlayerId) ?? throw new ResourceNotFound<Player>(request.PlayerId);

        var canUnenrollAfterSessionStart = await _permissionsService.Can(PermissionKey.Play_IgnoreSessionResetSettings);
        if (!canUnenrollAfterSessionStart && player.HasStartedSession)
            throw new SessionAlreadyStarted(request.PlayerId, "Non-admins can't unenroll from a game once they've started a session.");

        var teammateIds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p =>
                p.TeamId == player.TeamId &&
                p.Id != request.PlayerId
            )
            .Select(p => p.Id)
            .ToArrayAsync();

        if (teammateIds.Length > 0 && player.Role == PlayerRole.Manager)
            throw new ManagerCantUnenrollWhileTeammatesRemain(player.Id, player.TeamId, teammateIds);
    }

    private async Task<bool> GameExists(string id)
    {
        return
            id.NotEmpty() && await _store.AnyAsync<Data.Game>(g => g.Id == id, CancellationToken.None);
        ;
    }

    private async Task<bool> UserExists(string id)
    {
        return
            id.NotEmpty() && await _store.AnyAsync<Data.User>(u => u.Id == id, CancellationToken.None);
        ;
    }
}
