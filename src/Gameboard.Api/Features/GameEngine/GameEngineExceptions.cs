using System;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.GameEngine;

internal class GameEngineException(string message, Exception innerEx) : GameboardException(message, innerEx) { }

internal class GradingFailed : GameboardException
{
    public GradingFailed(string challengeId, Exception innerException) : base($"Grading failed for challenge {challengeId}: {innerException?.Message}", innerException) { }
}

internal class GamespaceStartFailure : GameboardException
{
    public GamespaceStartFailure(string gamespaceId, GameEngineType gameEngineType, Exception innerEx)
        : base($"Failed to start gamespace {gamespaceId} (engine: {gameEngineType})", innerEx) { }
}

internal class SubmissionIsForExpiredGamespace : GameboardValidationException
{
    public SubmissionIsForExpiredGamespace(string challengeId, Exception innerException) : base($"Couldn't make submission for challenge {challengeId}: The associated gamespace is expired.", innerException) { }
}

internal class TeamDoesntHaveChallenge : GameboardValidationException
{
    public TeamDoesntHaveChallenge(string teamId) : base($"Team {teamId} doesn't have any challenge data. This is probably because they haven't deployed a challenge yet.") { }
}
