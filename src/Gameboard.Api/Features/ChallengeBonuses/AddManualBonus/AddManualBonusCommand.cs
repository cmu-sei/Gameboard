using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(string challengeId, CreateManualChallengeBonus model) : IRequest;
