using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
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
    private readonly GameHasNoAwardedAutoBonuses<ConfigureGameAutoBonusesCommand> _gameHasNoAwardedBonuses;
    private readonly IGuidService _guids;
    private readonly IScoringService _scoringService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<ConfigureGameAutoBonusesCommand> _validatorService;

    public ConfigureGameAutoBonusesHandler(
        IChallengeStore challengeStore,
        IChallengeBonusStore challengeBonusStore,
        IChallengeSpecStore challengeSpecStore,
        EntityExistsValidator<Data.Game> gameExists,
        GameHasNoAwardedAutoBonuses<ConfigureGameAutoBonusesCommand> gameHasNoAwardedBonuses,
        IGuidService guids,
        IScoringService scoringService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<ConfigureGameAutoBonusesCommand> validatorService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameExists = gameExists;
        _gameHasNoAwardedBonuses = gameHasNoAwardedBonuses;
        _guids = guids;
        _scoringService = scoringService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<GameScoringConfig> Handle(ConfigureGameAutoBonusesCommand request, CancellationToken cancellationToken)
    {
        // authorize
        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Director, UserRole.Designer, UserRole.Tester };
        _userRoleAuthorizer.Authorize();

        // validate
        // game exists
        _validatorService.AddValidator(_gameExists.UseValue(request.Parameters.GameId));

        // grab the specs ahead of time to speed up validation, and we'll use them again later
        // NOTE: we're tracking them here since we're going to update them
        var specs = await _challengeSpecStore
            .ListAsNoTracking()
            .Include(s => s.Bonuses)
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
                .Select(b => b.SupportKey)
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

        // and go (with a transaction to maintain atomicity)
        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
        {
            foreach (var spec in specs)
            {
                // first, compose all bonuses for this spec:
                var newBonuses = new List<GameAutomaticBonusSolveRank>(request.Parameters.Config.AllChallengesBonuses);
                newBonuses.AddRange
                (
                    request
                        .Parameters
                        .Config
                        .SpecificChallengesBonuses
                        .Where(b => b.SupportKey == spec.Tag)
                        .Select(b => new GameAutomaticBonusSolveRank
                        {
                            Description = b.Description,
                            PointValue = b.PointValue,
                            SolveRank = b.SolveRank
                        })
                );

                // NOTE: ExecuteDeleteAsync seems to mess with the transaction stuff - may be a
                // postgres implementation problem
                //
                // then delete all existing bonuses from the db
                // await _challengeSpecStore
                //     .DbContext
                //     .ChallengeBonuses
                //     .Where(b => b.ChallengeSpecId == spec.Id)
                //     .ExecuteDeleteAsync();


                await UpdateDatabase(_challengeBonusStore.DbContext, spec.Id, newBonuses);
            }

            scope.Complete();
        }

        return await _scoringService.GetGameScoringConfig(request.Parameters.GameId);
    }

    private async Task UpdateDatabase(GameboardDbContext ctx, string specId, IEnumerable<GameAutomaticBonusSolveRank> bonuses)
    {
        var newBonusEntities = bonuses.Select(b => new Data.ChallengeBonusCompleteSolveRank
        {
            Id = _guids.GetGuid(),
            Description = b.Description,
            PointValue = b.PointValue,
            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
            AwardedTo = new List<AwardedChallengeBonus>(),
            ChallengeSpecId = specId
        } as Data.ChallengeBonus).ToArray();

        var currentBonuses = await ctx
            .ChallengeBonuses
            .Where(b => b.ChallengeSpecId == specId)
            .ToArrayAsync();

        ctx.RemoveRange(currentBonuses);

        // then insert new ones attached by specId
        ctx
            .ChallengeBonuses
            .AddRange(newBonusEntities);

        ctx.SaveChanges();
    }
}
