// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ConfigureGameAutoBonusesCommand(ConfigureGameAutoBonusesCommandParameters Parameters) : IRequest<GameScoringConfig>;

internal class ConfigureGameAutoBonusesHandler(
    IGuidService guids,
    IScoringService scoringService,
    IStore store,
    IGameboardRequestValidator<ConfigureGameAutoBonusesCommand> requestValidator) : IRequestHandler<ConfigureGameAutoBonusesCommand, GameScoringConfig>
{
    private readonly IGuidService _guids = guids;
    private readonly IGameboardRequestValidator<ConfigureGameAutoBonusesCommand> _requestValidator = requestValidator;
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;

    public async Task<GameScoringConfig> Handle(ConfigureGameAutoBonusesCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _requestValidator
            .Validate(request, cancellationToken);

        // and go (with a transaction to maintain atomicity)
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
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

                // NOTE: ExecuteDeleteAsync seems to mess with the transaction stuff - may be a
                // postgres implementation problem, or may be by design
                //
                // then delete all existing bonuses from the db
                // await _challengeSpecStore
                //     .DbContext
                //     .ChallengeBonuses
                //     .Where(b => b.ChallengeSpecId == spec.Id)
                //     .ExecuteDeleteAsync();
                await UpdateDatabase(spec.Id, newBonuses);
            }

            scope.Complete();
        }

        return await _scoringService.GetGameScoringConfig(request.Parameters.GameId);
    }

    private async Task UpdateDatabase(string specId, IEnumerable<GameAutomaticBonusSolveRank> bonuses)
    {
        var newBonusEntities = bonuses.Select(b => new Data.ChallengeBonusCompleteSolveRank
        {
            Id = _guids.Generate(),
            Description = b.Description,
            PointValue = b.PointValue,
            SolveRank = b.SolveRank,
            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
            AwardedTo = [],
            ChallengeSpecId = specId
        } as Data.ChallengeBonus).ToArray();

        var currentBonuses = await _store
            .WithTracking<Data.ChallengeBonus>()
            .Where(b => b.ChallengeSpecId == specId)
            .ToArrayAsync();

        // delete old bonuses
        await _store.Delete(currentBonuses);

        // then insert new ones attached by specId
        await _store.SaveAddRange(newBonusEntities);
    }
}
