using System;
using System.Text.Json;
using AutoMapper;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Features.GameEngine;

internal static class GameEngineChallengeExtensions
{
    public static GameEngineGameState BuildGameEngineState(this Data.Challenge challenge, IMapper mapper, JsonSerializerOptions jsonSerializerOptions)
    {
        var state = mapper.Map<GameState, GameEngineGameState>(JsonSerializer.Deserialize<TopoMojo.Api.Client.GameState>(challenge.State, jsonSerializerOptions));
        state.Vms ??= Array.Empty<GameEngineVmState>();

        return state;
    }
}
