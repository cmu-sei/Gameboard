using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ConfigureGameAutoBonusesCommand(ConfigureGameAutoBonusesCommandParameters Parameters) : IRequest<GameScoringConfig>;

internal class ConfigureGameAutoBonusesHandler : IRequestHandler<ConfigureGameAutoBonusesCommand, GameScoringConfig>
{
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IGuidService _guids;
    private readonly IScoringService _scoringService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IGameboardValidator<ConfigureGameAutoBonusesCommand> _validator;
    private readonly IValidatorService<ConfigureGameAutoBonusesCommand> _validatorService;

    public ConfigureGameAutoBonusesHandler(
        IChallengeBonusStore challengeBonusStore,
        IStore<Data.ChallengeSpec> challengeSpecStore,
        IGuidService guids,
        IScoringService scoringService,
        UserRoleAuthorizer userRoleAuthorizer,
        IGameboardValidator<ConfigureGameAutoBonusesCommand> validator,
        IValidatorService<ConfigureGameAutoBonusesCommand> validatorService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeSpecStore = challengeSpecStore;
        _guids = guids;
        _scoringService = scoringService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
        _validatorService = validatorService;
    }

    public async Task<GameScoringConfig> Handle(ConfigureGameAutoBonusesCommand request, CancellationToken cancellationToken)
    {
        // authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Designer, UserRole.Tester)
            .Authorize();

        // validate
        _validatorService.AddValidator(_validator);
        await _validatorService.Validate(request, cancellationToken);

        // and go (with a transaction to maintain atomicity)
        var specs = await _challengeSpecStore
            .ListAsNoTracking()
            .Where(s => s.GameId == request.Parameters.GameId)
            .Select(s => new { s.Id, s.Tag })
            .ToArrayAsync(cancellationToken);

        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
        {
            foreach (var spec in specs)
            {
                var newBonuses = new List<GameAutomaticBonusSolveRank>(request.Parameters.Config.AllChallengesBonuses);
                if (request.Parameters.Config.SpecificChallengesBonuses?.Any() ?? false)
                {
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
                }

                await UpdateDatabase(_challengeBonusStore.DbContext, spec.Id, newBonuses);
            }

            // NOTE: ExecuteDeleteAsync seems to mess with the transaction stuff - may be a
            // postgres implementation problem, or may be by design
            //
            // then delete all existing bonuses from the db
            // await _challengeSpecStore
            //     .DbContext
            //     .ChallengeBonuses
            //     .Where(b => b.ChallengeSpecId == spec.Id)
            //     .ExecuteDeleteAsync();

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
