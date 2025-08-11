using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record ListPracticeChallengeGroupsQuery(ListPracticeChallengeGroupsRequest Request) : IRequest<ListPracticeChallengeGroupsResponse>;

internal sealed class ListPracticeChallengeGroupsHandler
(
    IPracticeService practiceService,
    IValidatorService validatorService
) : IRequestHandler<ListPracticeChallengeGroupsQuery, ListPracticeChallengeGroupsResponse>
{
    public async Task<ListPracticeChallengeGroupsResponse> Handle(ListPracticeChallengeGroupsQuery request, CancellationToken cancellationToken)
    {
        await validatorService
            // ANON OKAY, because practice, no .Auth call
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.Request.ParentGroupId, false)
            .Validate(cancellationToken);

        var groups = await practiceService.ChallengeGroupsList(new ChallengeGroupsListArgs
        {
            GetRootOnly = request.Request.GetRootOnly,
            ParentGroupId = request.Request.ParentGroupId,
            SearchTerm = request.Request.SearchTerm
        }, cancellationToken);

        return new ListPracticeChallengeGroupsResponse { Groups = groups };
        // // one of the properties we need to compute is whether the user is eligible for a certificate
        // // for any completed challenge. to do this, we need to know if there's a global cert for the practice area
        // var requestedParentGroupId = request.Request.ParentGroupId.IsEmpty() ? null : request.Request.ParentGroupId;
        // var requestedSearchTerm = request.Request.SearchTerm.IsEmpty() ? null : request.Request.SearchTerm;
        // var practiceSettings = await practiceService.GetSettings(cancellationToken);
        // var hasGlobalCertificate = practiceSettings.CertificateTemplateId.IsNotEmpty();

        // // now pull the challenge groups and their challenges (specs)
        // var challengeGroups = await store
        //     .WithNoTracking<PracticeChallengeGroup>()
        //     .Where
        //     (
        //         g =>
        //             (request.Request.GetRootOnly && g.ParentGroupId == null) ||
        //             (g.ParentGroupId == requestedParentGroupId)
        //     )
        //     .Where
        //     (
        //         g =>
        //             requestedSearchTerm == null ||
        //             g.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)) ||
        //             g.ChallengeSpecs.Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm))) ||
        //             g.ChildGroups.SelectMany(cg => cg.ChallengeSpecs).Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
        //     )
        //     // we materialize an anonymous type because we have to do a lot of funky aggregation that we don't need to return
        //     // all the data from (e.g. tags)
        //     .Select(g => new
        //     {
        //         g.Id,
        //         g.Name,
        //         g.Description,
        //         g.ImageUrl,
        //         g.IsFeatured,
        //         g.TextSearchVector,
        //         ParentGroup = g.ParentGroupId != null ? new SimpleEntity { Id = g.ParentGroupId, Name = g.ParentGroup.Name } : null,
        //         ChildGroups = g.ChildGroups.Select(c => new
        //         {
        //             c.Id,
        //             c.Name,
        //             ChallengeSpecs = c.ChallengeSpecs.Select(s => new { s.Id, Tags = s.Tags.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) }).ToArray(),
        //         }).ToArray(),
        //         ChallengeCount = g.ChallengeSpecs.Count + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Count(),
        //         ChallengeMaxScoreTotal = g.ChallengeSpecs.Select(s => s.Points).Sum() + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Select(s => s.Points).Sum(),
        //         Challenges = g.ChallengeSpecs
        //             .Where(s => !s.IsHidden && !s.Disabled)
        //             .Select(s => new PracticeChallengeGroupDtoChallenge
        //             {
        //                 Id = s.Id,
        //                 Name = s.Name,
        //                 Description = s.Description,
        //                 MaxPossibleScore = s.Points,
        //                 // we have to parse the tags out and filter them by practice area settings later, but
        //                 // can't do that in the EF query context. .split works here, but will happen on retrieval
        //                 Tags = s.Tags.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
        //                 // we also need data about the user's attempts, but we'll get that later because #317
        //             })
        //             .ToArray(),
        //     })
        //     .OrderBy(c => c.IsFeatured ? 0 : 1)
        //         .ThenByDescending(g => requestedSearchTerm == null ? 1 : g.TextSearchVector.Rank(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
        //         .ThenBy(c => c.Name)
        //     .ToArrayAsync(cancellationToken);

        // // shortcut out if there are no groups
        // if (challengeGroups.Length == 0)
        // {
        //     return new ListPracticeChallengeGroupsResponse { Groups = [] };
        // }

        // // otherwise, we have to do some work
        // // first, we need to screen out tags that are on these challenges/groups but shouldn't show to end users
        // // (filtered by practice area settings)
        // var groupTagsDict = new Dictionary<string, string[]>();
        // var challenges = challengeGroups.SelectMany(g => g.Challenges).ToArray();
        // var visibleTags = new HashSet<string>(await practiceService.GetVisibleChallengeTags(cancellationToken));

        // foreach (var group in challengeGroups)
        // {
        //     var groupTags = new List<string>(group.Challenges.SelectMany(c => c.Tags));
        //     groupTags.AddRange(group.ChildGroups.SelectMany(g => g.ChallengeSpecs.SelectMany(s => s.Tags)));
        //     groupTagsDict.Add(group.Id, [.. visibleTags.Intersect(groupTags.Distinct().OrderBy(t => t))]);

        //     foreach (var challenge in challenges)
        //     {
        //         challenge.Tags = [.. challenge.Tags.Intersect(visibleTags).OrderBy(t => t)];
        //     }
        // }

        // return new ListPracticeChallengeGroupsResponse
        // {
        //     Groups = challengeGroups.Select(g => new PracticeChallengeGroupDto
        //     {
        //         Id = g.Id,
        //         Name = g.Name,
        //         Description = g.Description,
        //         ImageUrl = g.ImageUrl,
        //         IsFeatured = g.IsFeatured,
        //         ChallengeCount = g.ChallengeCount,
        //         ChallengeMaxScoreTotal = g.ChallengeMaxScoreTotal,
        //         Challenges = g.Challenges,
        //         ChildGroups = [.. g.ChildGroups.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })],
        //         ParentGroup = g.ParentGroup,
        //         Tags = groupTagsDict.GetValueOrDefault(g.Id) ?? []
        //     })
        //     .ToArray()
        // };
    }
}
