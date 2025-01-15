using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public interface IAutoTagService
{
    public Task<IEnumerable<string>> GetAutoTags(Data.Ticket ticket, CancellationToken cancellationToken);
}

internal sealed class AutoTagService(IStore store) : IAutoTagService
{
    private readonly IStore _store = store;

    public async Task<IEnumerable<string>> GetAutoTags(Data.Ticket ticket, CancellationToken cancellationToken)
    {
        var retVal = new List<string>();

        var teamSponsorIds = Array.Empty<string>();
        if (!ticket.TeamId.IsNotEmpty())
        {
            teamSponsorIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == ticket.TeamId)
                .Select(p => p.SponsorId)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            retVal.AddRange
            (
                await _store
                    .WithNoTracking<SupportSettingsAutoTag>()
                    .Where(t => t.ConditionType == SupportSettingsAutoTagConditionType.SponsorId && teamSponsorIds.Contains(t.ConditionValue))
                    .Select(t => t.Tag)
                    .ToArrayAsync(cancellationToken)
            );
        }

        if (ticket.ChallengeId.IsNotEmpty())
        {
            var challengeData = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == ticket.ChallengeId)
                .Select(c => new
                {
                    c.GameId,
                    c.PlayerMode,
                    c.SpecId,
                })
                .SingleOrDefaultAsync(cancellationToken);

            var playerModeValue = (int)challengeData.PlayerMode;

            if (challengeData is not null)
            {
                retVal.AddRange
                (
                    await _store
                        .WithNoTracking<SupportSettingsAutoTag>()
                        .Where
                        (
                            c =>
                                (c.ConditionType == SupportSettingsAutoTagConditionType.GameId && c.ConditionValue == challengeData.GameId) ||
                                (c.ConditionType == SupportSettingsAutoTagConditionType.ChallengeSpecId && c.ConditionValue == challengeData.SpecId) ||
                                (c.ConditionType == SupportSettingsAutoTagConditionType.PlayerMode && c.ConditionValue == challengeData.PlayerMode.ToString())
                        )
                        .Select(c => c.Tag)
                        .ToArrayAsync(cancellationToken)
                );
            }

            if (ticket.TeamId.IsNotEmpty())
            {
                var teamGameIds = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.Id == ticket.TeamId)
                    .Select(p => p.GameId)
                    .Distinct()
                    .ToArrayAsync(cancellationToken);

                if (teamGameIds.Length > 0)
                {
                    retVal.AddRange
                    (
                        await _store
                            .WithNoTracking<SupportSettingsAutoTag>()
                            .Where(c => c.ConditionType == SupportSettingsAutoTagConditionType.GameId && teamGameIds.Contains(c.ConditionValue))
                            .Select(c => c.Tag)
                            .ToArrayAsync(cancellationToken)
                    );
                }
            }
        }

        return [.. retVal.Distinct().OrderBy(t => t)];
    }
}
