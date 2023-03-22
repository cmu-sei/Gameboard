using System.Collections.Generic;
using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ListManualBonusesQuery(string challengeId) : IRequest<IEnumerable<ManualChallengeBonusViewModel>>;
