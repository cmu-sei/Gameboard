using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class DeleteGameAutoBonusesConfigValidator : IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand>
{
    private readonly EntityExistsValidator<DeleteGameAutoBonusesConfigCommand, Data.Game> _gameExists;
    private Func<DeleteGameAutoBonusesConfigCommand, string> _gameIdPropertyExpression;
    private readonly IStore _store;
    private readonly IValidatorService<DeleteGameAutoBonusesConfigCommand> _validatorService;

    public DeleteGameAutoBonusesConfigValidator
    (
        EntityExistsValidator<DeleteGameAutoBonusesConfigCommand, Data.Game> gameExists,
        IStore store,
        IValidatorService<DeleteGameAutoBonusesConfigCommand> validatorService
    )
    {
        _gameExists = gameExists;
        _store = store;
        _validatorService = validatorService;
    }

    public IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand> UseGameIdProperty(Func<DeleteGameAutoBonusesConfigCommand, string> gameIdPropertyExpression)
    {
        _gameIdPropertyExpression = gameIdPropertyExpression;
        return this;
    }

    public async Task Validate(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        var gameId = _gameIdPropertyExpression(request);
        _validatorService.AddValidator(_gameExists.UseProperty(_gameIdPropertyExpression));
        _validatorService.AddValidator
        (
            async (req, context) =>
            {
                var awardedBonusIds = await _store
                    .ListAsNoTracking<AwardedChallengeBonus>()
                    .Include(ab => ab.ChallengeBonus)
                        .ThenInclude(b => b.ChallengeSpec)
                    .Where(b => b.ChallengeBonus.ChallengeSpec.GameId == gameId)
                    .Select(ab => ab.ChallengeBonusId)
                    .ToArrayAsync(cancellationToken);

                if (awardedBonusIds.Length > 0)
                    context.AddValidationException(new CantDeleteAutoBonusIfAwarded(gameId, awardedBonusIds));
            }
        );

        await _validatorService.Validate(request, cancellationToken);
    }
}
