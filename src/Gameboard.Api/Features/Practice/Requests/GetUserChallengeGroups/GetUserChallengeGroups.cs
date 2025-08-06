using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using MediatR;
using System;
using System.Collections.Generic;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Features.Users;
using ServiceStack;

namespace Gameboard.Api.Features.Practice;

public record GetUserChallengeGroupsQuery(string UserId, string GroupId, string ParentGroupId, string SearchTerm) : IRequest<GetUserChallengeGroupsResponse>;

internal sealed class GetUserChallengeGroupsHandler
(
    IActingUserService actingUserService,
    INowService nowService,
    IUserRolePermissionsService permissionsService,
    IPracticeService practiceService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetUserChallengeGroupsQuery, GetUserChallengeGroupsResponse>
{
    public async Task<GetUserChallengeGroupsResponse> Handle(GetUserChallengeGroupsQuery request, CancellationToken cancellationToken)
    {
        // the acting user could also be null, because we don't make them log in to see challenge collections
        var actingUser = actingUserService.Get();

        await validatorService
            // ANON OKAY, because practice, no .Auth call
            .AddEntityExistsValidator<Data.User>(request.UserId, false)
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.ParentGroupId, false)
            .AddValidator(async ctx =>
            {
                // this doesn't matter if we're just pulling for the current user or if we're not logged in
                if (request.UserId.IsEmpty() || actingUser is null)
                {
                    return;
                }

                // if we're pulling for a specific user, we need to either be that user or be someone with permission
                if (request.UserId == actingUser.Id)
                {
                    return;
                }

                if (!await permissionsService.Can(PermissionKey.Admin_View))
                {
                    ctx.AddValidationException(new CantAccessUserChallengeGroupDataException(request.UserId));
                }
            })
            .Validate(cancellationToken);

        // one of the properties we need to compute is whether the user is eligible for a certificate
        // for any completed challenge. to do this, we need to know if there's a global cert for the practice area
        var requestedGroupId = request.GroupId.IsEmpty() ? null : request.GroupId;
        var requestedParentGroupId = request.ParentGroupId.IsEmpty() ? null : request.ParentGroupId;
        var requestedSearchTerm = request.SearchTerm.IsEmpty() ? null : request.SearchTerm;
        var practiceSettings = await practiceService.GetSettings(cancellationToken);
        var hasGlobalCertificate = practiceSettings.CertificateTemplateId.IsNotEmpty();

        // now pull the challenge groups and their challenges (specs)
        var challengeGroups = await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => requestedGroupId == null || g.Id == requestedGroupId)
            .Where
            (
                g =>
                    (g.ParentGroupId == null && requestedParentGroupId == null) ||
                    (g.ParentGroupId == requestedParentGroupId)
            )
            .Where
            (
                g =>
                    requestedSearchTerm == null ||
                    g.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)) ||
                    g.ChallengeSpecs.Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm))) ||
                    g.ChildGroups.SelectMany(cg => cg.ChallengeSpecs).Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
            )
            // we materialize an anonymous type because we have to do a lot of funky aggregation that we don't need to return
            // all the data from (e.g. tags)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.ImageUrl,
                g.IsFeatured,
                g.TextSearchVector,
                ParentGroup = g.ParentGroupId != null ? new SimpleEntity { Id = g.ParentGroupId, Name = g.ParentGroup.Name } : null,
                ChildGroups = g.ChildGroups.Select(c => new
                {
                    c.Id,
                    c.Name,
                    ChallengeSpecs = c.ChallengeSpecs.Select(s => new { s.Id, Tags = s.Tags.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) }).ToArray(),
                }).ToArray(),
                ChallengeCount = g.ChallengeSpecs.Count + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Count(),
                ChallengePointTotal = g.ChallengeSpecs.Select(s => s.Points).Sum() + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Select(s => s.Points).Sum(),
                Challenges = g.ChallengeSpecs
                    .Where(s => !s.IsHidden && !s.Disabled)
                    .Select(s => new GetUserChallengeGroupsResponseChallenge
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        MaxPossibleScore = s.Points,
                        HasCertificateTemplate = hasGlobalCertificate || s.Game.PracticeCertificateTemplateId != null,
                        // we have to parse the tags out and filter them by practice area settings later, but
                        // can't do that in the EF query context. .split works here, but will happen on retrieval
                        Tags = s.Tags.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                        // we also need data about the user's attempts, but we'll get that later because #317
                        BestAttempt = null
                    })
                    .ToArray(),
            })
            .OrderBy(c => c.IsFeatured ? 0 : 1)
                .ThenByDescending(g => requestedSearchTerm == null ? 1 : g.TextSearchVector.Rank(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
                .ThenBy(c => c.Name)
            .ToArrayAsync(cancellationToken);

        // shortcut out if there are no groups
        if (challengeGroups.Length == 0)
        {
            return new GetUserChallengeGroupsResponse { Groups = [] };
        }

        // otherwise, we have to do some work
        // first, we need to screen out tags that are on these challenges/groups but shouldn't show to end users
        // (filtered by practice area settings)
        var groupTagsDict = new Dictionary<string, string[]>();
        var challenges = challengeGroups.SelectMany(g => g.Challenges).ToArray();
        var visibleTags = new HashSet<string>(await practiceService.GetVisibleChallengeTags(cancellationToken));

        foreach (var group in challengeGroups)
        {
            var groupTags = new List<string>(group.Challenges.SelectMany(c => c.Tags));
            groupTags.AddRange(group.ChildGroups.SelectMany(g => g.ChallengeSpecs.SelectMany(s => s.Tags)));
            groupTagsDict.Add(group.Id, [.. visibleTags.Intersect(groupTags.Distinct().OrderBy(t => t))]);

            foreach (var challenge in challenges)
            {
                challenge.Tags = [.. challenge.Tags.Intersect(visibleTags).OrderBy(t => t)];
            }
        }

        // if we have a requested user, we also need to pull info about their best attempts on this content
        if (request.UserId.IsNotEmpty())
        {
            var nowish = nowService.Get();
            var challengeIds = challenges.Select(c => c.Id).ToArray();

            var challengeData = await store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Player.UserId == request.UserId)
                .Where(c => c.EndTime <= nowish)
                .Where(c => challengeIds.Contains(c.SpecId))
                .GroupBy(c => c.SpecId)
                .Select(kv => new
                {
                    SpecId = kv.Key,
                    Challenge = kv.OrderByDescending(c => c.Score).Select(c => new
                    {
                        c.StartTime,
                        c.Score,
                    })
                    .FirstOrDefault()
                })
                .ToDictionaryAsync(kv => kv.SpecId, kv => kv.Challenge, cancellationToken);

            foreach (var challengeGroup in challengeGroups)
            {
                foreach (var challenge in challengeGroup.Challenges)
                {
                    if (challengeData.TryGetValue(challenge.Id, out var bestAttempt))
                    {
                        challenge.BestAttempt = new GetUserChallengeGroupsResponseChallengeAttempt
                        {
                            CertificateAwarded = bestAttempt.Score >= challenge.MaxPossibleScore && challenge.HasCertificateTemplate,
                            Date = bestAttempt.StartTime,
                            Score = bestAttempt.Score
                        };
                    }
                }
            }
        }

        return new GetUserChallengeGroupsResponse
        {
            Groups = challengeGroups.Select(g => new GetUserChallengeGroupsResponseGroup
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                ImageUrl = g.ImageUrl,
                IsFeatured = g.IsFeatured,
                ChallengeCount = g.ChallengeCount,
                ChallengePoints = g.ChallengePointTotal,
                Challenges = g.Challenges,
                ChildGroups = [.. g.ChildGroups.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })],
                ParentGroup = g.ParentGroup,
                Tags = groupTagsDict.GetValueOrDefault(g.Id) ?? [],
            })
            .ToArray()
        };
    }
}
