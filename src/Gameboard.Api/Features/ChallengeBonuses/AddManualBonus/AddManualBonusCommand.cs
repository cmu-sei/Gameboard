using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(CreateManualChallengeBonus model) : IRequest;
