using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using YamlDotNet.Core.Tokens;
using System;

namespace Gameboard.Api.Features.Support;

public sealed record GetSupportSettingsAutoTagsQuery() : IRequest<IEnumerable<SupportSettingsAutoTagViewModel>>;

internal sealed class GetSupportSettingsAutoTagsHandler(IStore store, IValidatorService validatorService) : IRequestHandler<GetSupportSettingsAutoTagsQuery, IEnumerable<SupportSettingsAutoTagViewModel>>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<IEnumerable<SupportSettingsAutoTagViewModel>> Handle(GetSupportSettingsAutoTagsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(b => b.RequirePermissions(Users.PermissionKey.Support_EditSettings))
            .Validate(cancellationToken);

        var autoTags = await _store
            .WithNoTracking<SupportSettingsAutoTag>()
            .Select(t => new SupportSettingsAutoTagViewModel
            {
                Id = t.Id,
                ConditionTypeDescription = string.Empty,
                ConditionType = t.ConditionType,
                ConditionValue = t.ConditionValue,
                Tag = t.Tag,
            })
            .ToArrayAsync(cancellationToken);

        // convert the values into something a little prettier for clients
        var challengeSpecs = new Dictionary<string, string>();
        var games = new Dictionary<string, string>();
        var sponsors = new Dictionary<string, string>();

        var specIds = autoTags
            .Where(t => t.ConditionType == SupportSettingsAutoTagConditionType.ChallengeSpecId)
            .Select(t => t.ConditionValue);

        if (specIds.Any())
        {
            challengeSpecs = await _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Where(s => specIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => $"{s.Game.Name} - {s.Name}", cancellationToken);
        }

        var gameIds = autoTags
            .Where(t => t.ConditionType == SupportSettingsAutoTagConditionType.GameId)
            .Select(t => t.ConditionValue);

        if (gameIds.Any())
        {
            games = await _store
                .WithNoTracking<Data.Game>()
                .Where(g => gameIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        }

        var sponsorIds = autoTags
            .Where(t => t.ConditionType == SupportSettingsAutoTagConditionType.SponsorId)
            .Select(t => t.ConditionValue);

        if (sponsorIds.Any())
        {
            sponsors = await _store
                .WithNoTracking<Data.Sponsor>()
                .Where(s => sponsorIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        }

        foreach (var autoTag in autoTags)
        {
            switch (autoTag.ConditionType)
            {
                case SupportSettingsAutoTagConditionType.ChallengeSpecId:
                    autoTag.ConditionTypeDescription = "Challenge";
                    autoTag.ConditionValue = challengeSpecs[autoTag.ConditionValue];
                    break;
                case SupportSettingsAutoTagConditionType.GameId:
                    autoTag.ConditionTypeDescription = "Game";
                    autoTag.ConditionValue = games[autoTag.ConditionValue];
                    break;
                case SupportSettingsAutoTagConditionType.PlayerMode:
                    var mode = (PlayerMode)int.Parse(autoTag.ConditionValue);
                    autoTag.ConditionTypeDescription = "Mode";
                    autoTag.ConditionValue = mode.ToString();
                    break;
                case SupportSettingsAutoTagConditionType.SponsorId:
                    autoTag.ConditionTypeDescription = "Sponsor";
                    autoTag.ConditionValue = sponsors[autoTag.ConditionValue];
                    break;
                default:
                    autoTag.ConditionTypeDescription = autoTag.ConditionType.ToString();
                    break;
            }
        }

        return autoTags;
    }
}
