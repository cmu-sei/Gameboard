using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ConfigureGameAutoBonusesValidator(
    EntityExistsValidator<ConfigureGameAutoBonusesCommand, Data.Game> gameExists,
    IStore store,
    IValidatorService<ConfigureGameAutoBonusesCommand> validatorService
    ) : IGameboardRequestValidator<ConfigureGameAutoBonusesCommand>
{
    private readonly EntityExistsValidator<ConfigureGameAutoBonusesCommand, Data.Game> _gameExists = gameExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<ConfigureGameAutoBonusesCommand> _validatorService = validatorService;

    public async Task Validate(ConfigureGameAutoBonusesCommand request, CancellationToken cancellationToken)
    {
        _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(PermissionKey.Games_ConfigureChallenges))
            .AddValidator(_gameExists.UseProperty(r => r.Parameters.GameId));

        // we're going to bulldoze all existing configuration for now to make this simpler, so we need to
        // ensure that there aren't any existing bonuses which have been awarded to a team for this game
        _validatorService.AddValidator
        (
            async (request, context) =>
            {
                var bonusesAwarded = await _store
                    .WithNoTracking<Data.AwardedChallengeBonus>()
                        .Include(b => b.Challenge)
                    .Where(b => b.Challenge.GameId == request.Parameters.GameId)
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);

                foreach (var bonusAwarded in bonusesAwarded)
                    context.AddValidationException(new CantDeleteAutoBonusIfAwarded(request.Parameters.GameId, bonusesAwarded));
            }
        );

        _validatorService.AddValidator
        (
            async (request, context) =>
            {
                var allPointValues = new List<double>();

                if (request.Parameters.Config.AllChallengesBonuses is not null && request.Parameters.Config.AllChallengesBonuses.Any())
                {
                    allPointValues.AddRange(request.Parameters.Config.AllChallengesBonuses.Select(b => b.PointValue));
                }

                if (request.Parameters.Config.SpecificChallengesBonuses is not null && request.Parameters.Config.SpecificChallengesBonuses.Any())
                {
                    allPointValues.AddRange(request.Parameters.Config.SpecificChallengesBonuses.Select(b => b.PointValue));

                    var specs = await _store
                        .WithNoTracking<Data.ChallengeSpec>()
                        .Include(s => s.Bonuses)
                        .Where(s => s.GameId == request.Parameters.GameId)
                        .ToArrayAsync();

                    // all specifically-configured challenges have existing support keys
                    var challengeSupportKeys = specs.Select(s => s.Tag).ToArray();

                    if (challengeSupportKeys != null && challengeSupportKeys.Length > 0)
                    {
                        var nonExistentKeys = request
                            .Parameters
                            .Config
                            .SpecificChallengesBonuses
                            .Select(b => b.SupportKey)
                            .Where(k => !challengeSupportKeys.Contains(k))
                            .ToArray();

                        foreach (var key in nonExistentKeys)
                            context.AddValidationException(new NonExistentSupportKey(key));
                    }
                }

                // point values all > 0
                if (allPointValues.Any(v => v <= 0))
                    context.AddValidationException(new GameAutoBonusCantBeNonPositive(request.Parameters.GameId, allPointValues.ToArray()));
            }
        );

        await _validatorService.Validate(request, cancellationToken);
    }
}
