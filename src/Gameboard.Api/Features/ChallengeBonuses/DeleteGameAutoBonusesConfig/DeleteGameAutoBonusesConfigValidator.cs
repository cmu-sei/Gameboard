// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class DeleteGameAutoBonusesConfigValidator : IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand>
{
    private readonly EntityExistsValidator<DeleteGameAutoBonusesConfigCommand, Data.Game> _gameExists;
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

    public async Task Validate(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.Require(PermissionKey.Games_CreateEditDelete))
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator
            (
                async (req, context) =>
                {
                    var awardedBonusIds = await _store
                        .WithNoTracking<AwardedChallengeBonus>()
                        .Include(ab => ab.ChallengeBonus)
                            .ThenInclude(b => b.ChallengeSpec)
                        .Where(b => b.ChallengeBonus.ChallengeSpec.GameId == request.GameId)
                        .Select(ab => ab.ChallengeBonusId)
                        .ToArrayAsync(cancellationToken);

                    if (awardedBonusIds.Length > 0)
                        context.AddValidationException(new CantDeleteAutoBonusIfAwarded(request.GameId, awardedBonusIds));
                }
            ).Validate(request, cancellationToken);
    }
}
