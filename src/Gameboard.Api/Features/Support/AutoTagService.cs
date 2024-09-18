using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public interface IAutoTagService
{
    public Task<IEnumerable<string>> GetAutoTags(Data.Ticket ticket, CancellationToken cancellationToken);
}

internal sealed class AutoTagService(IStore store, ITeamService teamService) : IAutoTagService
{
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;

    public async Task<IEnumerable<string>> GetAutoTags(Data.Ticket ticket, CancellationToken cancellationToken)
    {
        if (ticket.ChallengeId.IsEmpty() && ticket.TeamId.IsEmpty())
            return [];

        var teamSponsorIds = Array.Empty<string>();

        if (ticket.TeamId.IsEmpty())
        {
            teamSponsorIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == ticket.TeamId)
                .Select(p => p.SponsorId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
        }

        var challengeData = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == ticket.ChallengeId)
            .Select(c => new
            {
                c.PlayerMode,
                c.SpecId,
                c.GameId
            })
            .SingleAsync(cancellationToken);

        var autoTagConfig = await _store
            .WithNoTracking<SupportSettingsAutoTag>()
            .Where(t => t.IsEnabled)
            .Where
            (
                t =>
                    (t.ConditionType == SupportSettingsAutoTagConditionType.ChallengeSpecId && t.ConditionValue == challengeData.SpecId) ||
                    (t.ConditionType == SupportSettingsAutoTagConditionType.GameId && t.ConditionValue == challengeData.GameId) ||
                    (t.ConditionType == SupportSettingsAutoTagConditionType.PlayerMode && t.ConditionValue == challengeData.PlayerMode.ToString()) ||
                    (t.ConditionType == SupportSettingsAutoTagConditionType.SponsorId && teamSponsorIds.Contains(t.ConditionValue))
            )
            .Select(t => new
            {
                t.ConditionType,
                t.ConditionValue,
                t.Tag
            })
            .ToArrayAsync(cancellationToken);

        return autoTagConfig.Select(t => t.Tag).Distinct().ToArray();
    }
}
