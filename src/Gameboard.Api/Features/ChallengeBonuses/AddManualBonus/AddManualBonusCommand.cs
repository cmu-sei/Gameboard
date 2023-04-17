using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(string ChallengeId, CreateManualChallengeBonus Model) : IRequest;
