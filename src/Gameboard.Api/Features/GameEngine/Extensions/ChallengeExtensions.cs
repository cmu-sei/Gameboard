using System.Collections;
using System.Text.Json;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.GameEngine;

internal static class GameEngineChallengeExtensions
{
    public static GameEngineGameState BuildGameEngineState(this Data.Challenge challenge, JsonSerializerOptions jsonSerializerOptions)
    {
        return JsonSerializer.Deserialize<GameEngineGameState>(challenge.State, jsonSerializerOptions);
    }
}
