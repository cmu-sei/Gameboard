using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ConfigureGameAutoBonusesCommand(ConfigureGameAutoBonusesCommandParameters Parameters) : IRequest<GameScoringConfig>;

internal class ConfigureGameAutoBonusesHandler : IRequestHandler<ConfigureGameAutoBonusesCommand, GameScoringConfig>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly EntityExistsValidator<Data.Game> _gameExists;
    private readonly IGuidService _guids;
    private readonly IScoringService _scoringService;
    private readonly IValidatorService<ConfigureGameAutoBonusesCommand> _validatorService;

    public ConfigureGameAutoBonusesHandler(
        IChallengeStore challengeStore,
        IChallengeBonusStore challengeBonusStore,
        IChallengeSpecStore challengeSpecStore,
        EntityExistsValidator<Data.Game> gameExists,
        IGuidService guids,
        IScoringService scoringService,
        IValidatorService<ConfigureGameAutoBonusesCommand> validatorService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameExists = gameExists;
        _guids = guids;
        _scoringService = scoringService;
        _validatorService = validatorService;
    }

    public async Task<GameScoringConfig> Handle(ConfigureGameAutoBonusesCommand request, CancellationToken cancellationToken)
    {
        // validate
        // game exists
        _validatorService.AddValidator(_gameExists.UseValue(request.Parameters.GameId));

        // grab the specs ahead of time to speed up validation, and we'll use them again later
        var specs = await _challengeSpecStore
            .ListAsNoTracking()
            .Where(s => s.GameId == request.Parameters.GameId)
            .ToArrayAsync();

        // all specifically-configured challenges have existing support keys
        _validatorService.AddValidator((req, context) =>
        {
            var challengeSupportKeys = specs.Select(s => s.Tag).ToArray();
            var nonExistentKeys = req
                .Parameters
                .Config
                .SpecificChallengesBonuses
                .Select(b => b.ChallengeSupportKey)
                .Where(k => !challengeSupportKeys.Contains(k))
                .ToArray();

            foreach (var key in nonExistentKeys)
                context.AddValidationException(new NonExistentSupportKey(key));
        });

        // all point values are greater than zero
        _validatorService.AddValidator((req, context) =>
        {
            var allPointValues = req.Parameters.Config.AllChallengesBonuses.Select(b => b.PointValue);
            if (allPointValues.Any(v => v <= 0))
                context.AddValidationException(new GameAutoBonusCantBeNonPositive(req.Parameters.GameId, allPointValues.ToArray()));
        });

        // we're going to bulldoze all existing configuration for now to make this simpler, so we need to
        // ensure that there aren't any existing bonuses which have been awarded to a team for this game
        // TODO

        await _validatorService.Validate(request);

        // and go
        // for now

        foreach (var spec in specs)
        {
            var bonusesForThisSpec = new List<GameAutomaticBonusSolveRank>(request.Parameters.Config.AllChallengesBonuses);
            bonusesForThisSpec.AddRange
            (
                request
                    .Parameters
                    .Config
                    .SpecificChallengesBonuses
                    .Where(b => b.ChallengeSupportKey == spec.Tag)
                    .Select(b => new GameAutomaticBonusSolveRank
                    {
                        Description = b.Description,
                        PointValue = b.PointValue,
                        SolveRank = b.SolveRank
                    })
            );

            // intentionally clobber existing bonuses with our new ones
            spec.Bonuses = new Collection<Data.ChallengeBonus>
            (
                bonusesForThisSpec.Select(b => new Data.ChallengeBonusCompleteSolveRank
                {
                    Id = _guids.GetGuid(),
                    Description = b.Description,
                    PointValue = b.PointValue,
                    ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
                    AwardedTo = new List<AwardedChallengeBonus>()
                } as Data.ChallengeBonus).ToList()
            );

            await _challengeSpecStore.Update(spec);
        }

        return await _scoringService.GetGameScoringConfig(request.Parameters.GameId);
    }
}
