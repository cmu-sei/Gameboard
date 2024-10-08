// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.ChallengeGates;

public class ChallengeGateValidator(
    IStore store,
    IStore<Data.ChallengeSpec> specStore,
    IStore<Data.ChallengeGate> challengeGateStore) : IModelValidator
{
    private readonly IStore<Data.ChallengeSpec> _specStore = specStore;
    private readonly IStore<Data.ChallengeGate> _challengeGateStore = challengeGateStore;
    private readonly IStore _store = store;

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
        if ((await _challengeGateStore.Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<ChallengeGate>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(NewChallengeGate model)
    {
        if (!await _store.AnyAsync<Data.Game>(g => g.Id == model.GameId, default))
            throw new ResourceNotFound<Game>(model.GameId);

        if ((await _specStore.Exists(model.TargetId)).Equals(false))
            throw new ResourceNotFound<ChallengeSpec>(model.TargetId, "The target spec");

        if ((await _specStore.Exists(model.RequiredId)).Equals(false))
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
        if ((await _challengeGateStore.Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<ChallengeGate>(model.Id);

        await Task.CompletedTask;
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
