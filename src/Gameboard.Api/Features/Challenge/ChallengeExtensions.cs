using Gameboard.Api.Structure.Logging;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Challenges;

public static partial class ChallengeLoggingSourceGenerators
{
    [LoggerMessage(LogLevel.Information, EventId = LogEventId.Challenge_SyncWithGameEngine, Message = "Challenge {ChallengeId} for team {TeamId} synchronized with game engine. Gamespace on?: {IsGamespaceOn}")]
    public static partial void LogChallengeSync(this ILogger logger, string challengeId, string teamId, bool isGamespaceOn);
}
