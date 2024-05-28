// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators;

public class GameValidator : IModelValidator
{
    private readonly IStore _store;

    public GameValidator(IStore store)
    {
        _store = store;
    }

    public Task Validate(object model)
    {
        if (model is Entity)
            return _validate(model as Entity);

        if (model is ChangedGame)
            return _validate(model as ChangedGame);

        throw new ValidationTypeFailure<GameValidator>(model.GetType());
    }

    private Task _validate(ChangedGame game)
    {
        if (game.MinTeamSize > game.MaxTeamSize)
            throw new InvalidTeamSize(game.Id, game.Name, game.MinTeamSize, game.MaxTeamSize);

        if (game.GameStart.IsNotEmpty() && game.GameEnd.IsNotEmpty() && game.GameStart > game.GameEnd)
            throw new InvalidDateRange(new DateRange(game.GameStart, game.GameEnd));

        if (game.RegistrationType == GameRegistrationType.Open && game.RegistrationOpen > game.RegistrationClose)
            throw new InvalidDateRange(new DateRange(game.RegistrationOpen, game.RegistrationClose));

        return Task.CompletedTask;
    }

    private async Task _validate(Entity model)
    {
        if ((await Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<Data.Game>(model.Id);

        await Task.CompletedTask;
    }

    private async Task<bool> Exists(string id)
    {
        return id.IsNotEmpty() && await _store
            .WithNoTracking<Data.Game>()
            .AnyAsync(g => g.Id == id);
    }

}
