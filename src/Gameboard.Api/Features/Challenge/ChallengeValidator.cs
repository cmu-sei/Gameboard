// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators;

[DIAsTransient]
public class ChallengeValidator(IStore store) : IModelValidator
{
    private readonly IStore _store = store;

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
        if ((await _store.WithNoTracking<Data.Challenge>().AnyAsync(c => c.Id == model.Id)).Equals(false))
            throw new ResourceNotFound<Challenge>(model.Id);
    }

    private async Task _validate(NewChallenge model)
    {
        if ((await _store.AnyAsync<Data.Player>(p => p.Id == model.PlayerId, CancellationToken.None)).Equals(false))
            throw new ResourceNotFound<Data.Player>(model.PlayerId);

        if ((await _store.AnyAsync<Data.ChallengeSpec>(s => s.Id == model.SpecId, CancellationToken.None)).Equals(false))
            throw new ResourceNotFound<Data.ChallengeSpec>(model.SpecId);

        var player = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == model.PlayerId)
            .Select(p => new
            {
                p.Id,
                p.GameId,
                IsActive = p.SessionBegin >= DateTimeOffset.MinValue &&
                    p.SessionBegin <= DateTime.UtcNow &&
                    p.SessionEnd >= DateTime.UtcNow,
                IsPractice = p.Mode == PlayerMode.Practice,
            })
            .SingleAsync();

        if (!player.IsPractice && !player.IsActive)
            throw new SessionNotActive(player.Id);

        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => cs.Id == model.SpecId)
            .SingleOrDefaultAsync();

        if (spec.GameId != player.GameId)
            throw new ActionForbidden();

        // Note: not checking "already exists" since this is used idempotently
        await Task.CompletedTask;
    }

    private async Task _validate(ChangedChallenge model)
    {
        if ((await _store.AnyAsync<Data.Challenge>(c => c.Id == model.Id, CancellationToken.None)).Equals(false))
            throw new ResourceNotFound<Data.Challenge>(model.Id);

        await Task.CompletedTask;
    }
}
