using MediatR;

namespace Gameboard.Api.Features.Scores;

public record TeamChallengeScoreQuery(string challengeId) : IRequest<TeamChallengeScoreSummary>;