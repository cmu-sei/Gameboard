using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ConfigureGameAutoBonusesValidator : IGameboardValidator<ConfigureGameAutoBonusesCommand>
{
    private readonly EntityExistsValidator<ConfigureGameAutoBonusesCommand, Data.Game> _gameExists;
    private readonly IStore _store;

    public ConfigureGameAutoBonusesValidator
    (
        EntityExistsValidator<ConfigureGameAutoBonusesCommand, Data.Game> gameExists,
        IStore store
    )
    {
        _gameExists = gameExists;
        _store = store;
    }

    public Func<ConfigureGameAutoBonusesCommand, RequestValidationContext, Task> GetValidationTask()
    {
        return async (request, context) =>
        {
            await _gameExists
                .UseProperty(r => r.Parameters.GameId)
                .GetValidationTask()
                .Invoke(request, context);

            if (request.Parameters.Config.SpecificChallengesBonuses is not null && request.Parameters.Config.SpecificChallengesBonuses.Any())
            {
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

            // all point values are greater than zero
            var allPointValues = request.Parameters.Config.AllChallengesBonuses.Select(b => b.PointValue);
            if (allPointValues.Any(v => v <= 0))
                context.AddValidationException(new GameAutoBonusCantBeNonPositive(request.Parameters.GameId, allPointValues.ToArray()));

            // we're going to bulldoze all existing configuration for now to make this simpler, so we need to
            // ensure that there aren't any existing bonuses which have been awarded to a team for this game
            // TODO
        };
    }
}
