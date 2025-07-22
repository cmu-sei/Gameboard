using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record AddChallengesToGroupCommand(string ChallengeGroupId, AddChallengesToGroupRequest Request) : IRequest<AddChallengesToGroupResponse>;

internal sealed class AddChallengesToGroupHandler
(
    ChallengeSpecService challengeSpecService,
    IStore store,
    ValidatorService validator
) : IRequestHandler<AddChallengesToGroupCommand, AddChallengesToGroupResponse>
{
    public async Task<AddChallengesToGroupResponse> Handle(AddChallengesToGroupCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.ChallengeGroupId)
            .AddEntityExistsValidator<Data.Game>(request.Request.AddByGameId, false)
            .AddValidator(async ctx =>
            {
                if (request.Request.AddBySpecIds.IsEmpty())
                {
                    return;
                }

                var distinctIds = request.Request.AddBySpecIds.Distinct().ToArray();
                var existingIds = await store
                    .WithNoTracking<Data.ChallengeSpec>()
                    .Where(s => request.Request.AddBySpecIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToArrayAsync(cancellationToken);

                var nonExistentIds = distinctIds.Where(id => !existingIds.Contains(id)).ToArray();
                foreach (var id in nonExistentIds)
                {
                    ctx.AddValidationException(new ResourceNotFound<ChallengeSpec>(id));
                }
            })
            .Validate(cancellationToken);

        // to accommodate the fact that we could be adding by gameid, tag, or specid, we'll just take the combined
        // distinct specIds implied by all three and add them to the group
        var specIds = new List<string>();

        if (request.Request.AddBySpecIds.IsNotEmpty())
        {
            specIds.AddRange(request.Request.AddBySpecIds);
        }

        if (request.Request.AddByGameId.IsNotEmpty())
        {
            var specIdsFromGame = await challengeSpecService
                .GetPracticeEligibleQueryBase()
                .Where(s => s.GameId == request.Request.AddByGameId)
                .Select(s => s.Id)
                .ToArrayAsync(cancellationToken);

            specIds.AddRange(specIdsFromGame);
        }

        if (request.Request.AddByTag.IsNotEmpty())
        {
            var specsWithTags = await challengeSpecService
                .GetPracticeEligibleQueryBase()
                .Select(s => new { Id = s.Id, TagsRaw = s.Tags })
                .ToArrayAsync(cancellationToken);

            foreach (var spec in specsWithTags)
            {
                var tags = ChallengeSpecMapper.StringTagsToEnumerableStringTags(spec.TagsRaw);
                if (tags.Contains(request.Request.AddByTag))
                {
                    specIds.Add(spec.Id);
                }
            }
        }

        // can only be added to the group if they're not here already
        var groupSpecIds = await store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.PracticeChallengeGroups.Any(g => g.Id == request.ChallengeGroupId))
            .Select(s => s.Id)
            .ToArrayAsync(cancellationToken);

        // the ids to be added are the result of all the request parameters, distinctified, and
        // not already present in the group
        var finalSpecIds = specIds
            .Where(sId => !groupSpecIds.Contains(sId))
            .Distinct()
            .ToArray();

        // have to do this with proper EF because we don't have a real entity for the many-to-many
        var specs = finalSpecIds.Select(sId => new Data.ChallengeSpec { Id = sId }).ToArray();
        await store.DoTransaction(async ctx =>
        {
            var group = await ctx.PracticeChallengeGroups.FindAsync(request.ChallengeGroupId, cancellationToken);

            foreach (var spec in specs)
            {
                ctx.Attach(spec);
                group.ChallengeSpecs.Add(spec);
            }

            await ctx.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new AddChallengesToGroupResponse { AddedChallengeSpecIds = finalSpecIds };
    }
}
