// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.ChallengeGates;

public class ChallengeGateValidator : IModelValidator
{
    private readonly IStore<Data.ChallengeGate> _store;

    public ChallengeGateValidator(IStore<Data.ChallengeGate> store)
    {
        _store = store;
    }

    public Task Validate(object model)
    {
        if (model is Entity)
            return _validate(model as Entity);

        if (model is NewChallengeGate)
            return _validate(model as NewChallengeGate);

        if (model is ChangedChallengeGate)
            return _validate(model as ChangedChallengeGate);

        throw new ValidationTypeFailure<ChallengeGateValidator>(model.GetType());
    }

    private async Task _validate(Entity model)
    {
        if ((await Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<ChallengeGate>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(NewChallengeGate model)
    {
        if ((await GameExists(model.GameId)).Equals(false))
            throw new ResourceNotFound<Game>(model.GameId);

        if ((await SpecExists(model.TargetId)).Equals(false))
            throw new ResourceNotFound<ChallengeSpec>(model.TargetId, "The target spec");

        if ((await SpecExists(model.RequiredId)).Equals(false))
            throw new ResourceNotFound<ChallengeSpec>(model.RequiredId, "The required spec");

        string cycleDescription = DetectCycles(model.TargetId, model.RequiredId);
        if (!string.IsNullOrWhiteSpace(cycleDescription))
        {
            throw new CyclicalGateConfiguration(model.TargetId, model.RequiredId, cycleDescription);
        }

        await Task.CompletedTask;
    }

    private async Task _validate(ChangedChallengeGate model)
    {
        if ((await Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<ChallengeGate>(model.Id);

        await Task.CompletedTask;
    }

    private async Task<bool> Exists(string id)
    {
        return
            id.NotEmpty() &&
            (await _store.Retrieve(id)) is Data.ChallengeGate
        ;
    }

    private async Task<bool> GameExists(string id)
    {
        return
            id.NotEmpty() &&
            (await _store.DbContext.Games.FindAsync(id)) is Data.Game
        ;
    }

    private async Task<bool> SpecExists(string id)
    {
        return
            id.NotEmpty() &&
            (await _store.DbContext.ChallengeSpecs.FindAsync(id)) is Data.ChallengeSpec
        ;
    }

    // later, enhance with actual cycle detection
    // https://github.com/cmu-sei/Gameboard/issues/114
    internal string DetectCycles(string gameId, string targetId)
    {
        if (gameId == targetId)
        {
            return $"{gameId} => {targetId}";
        }

        return null;
    }
}
