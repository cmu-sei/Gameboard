using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteManualBonusCommand(string ManualBonusId) : IRequest;
