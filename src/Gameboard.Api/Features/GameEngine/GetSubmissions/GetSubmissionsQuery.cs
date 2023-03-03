using System.Collections.Generic;
using MediatR;

namespace Gameboard.Api.Features.GameEngine.Requests;

public record GetSubmissionsQuery(string teamId, string challengeId) : IRequest<IEnumerable<GameEngineSectionSubmission>>;
