using System.Collections.Generic;
using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ListManualBonusesQuery(string ChallengeId) : IRequest<IEnumerable<ManualChallengeBonusViewModel>>;
