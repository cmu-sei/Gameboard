using MediatR;

namespace Gameboard.Api.Features.Scores;

public record TeamGameScoreQuery(string teamId) : IRequest<TeamGameScoreSummary>;
