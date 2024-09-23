using System.Collections.Generic;
using MediatR;

namespace Gameboard.Api.Features.GameEngine;

public record GetSubmissionsQuery(string TeamId, string ChallengeId) : IRequest<IEnumerable<GameEngineSectionSubmission>>;
