using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record SearchPracticeChallengesQuery(SearchFilter Filter, bool? IsCompleted) : IRequest<SearchPracticeChallengesResult>;

internal class SearchPracticeChallengesHandler
(
    IActingUserService actingUser,
    IChallengeDocsService challengeDocsService,
    IPagingService pagingService,
    IUserRolePermissionsService permissionsService,
    IPracticeService practiceService,
    ISlugService slugger,
    IStore store
) : IRequestHandler<SearchPracticeChallengesQuery, SearchPracticeChallengesResult>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly IChallengeDocsService _challengeDocsService = challengeDocsService;
    private readonly IPagingService _pagingService = pagingService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly ISlugService _slugger = slugger;
    private readonly IStore _store = store;

    public async Task<SearchPracticeChallengesResult> Handle(SearchPracticeChallengesQuery request, CancellationToken cancellationToken)
    {
        // load settings - we need these to make decisions about tag-based matches
        var settings = await _practiceService.GetSettings(cancellationToken);
        var sluggedSuggestedSearches = settings.SuggestedSearches.Select(_slugger.Get);
        var hasGlobalPracticeCertificate = settings.CertificateTemplateId.IsNotEmpty();

        var query = await BuildQuery(request.Filter.Term, sluggedSuggestedSearches);
        var results = await query
            .Select(s => new PracticeChallengeView
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Text = s.Text,
                AverageDeploySeconds = s.AverageDeploySeconds,
                HasCertificateTemplate = hasGlobalPracticeCertificate || s.Game.PracticeCertificateTemplateId != null,
                IsHidden = s.IsHidden,
                ScoreMaxPossible = s.Points,
                SolutionGuideUrl = s.SolutionGuideUrl,
                Tags = ChallengeSpecMapper.StringTagsToEnumerableStringTags(s.Tags),
                Game = new PracticeChallengeViewGame
                {
                    Id = s.Game.Id,
                    Name = s.Game.Name,
                    Logo = s.Game.Logo,
                    IsHidden = !s.Game.IsPublished
                }
            })
            .ToArrayAsync(cancellationToken);

        // load the user history so we can reflect their progress on all challenges
        var userHistory = Array.Empty<UserPracticeHistoryChallenge>();
        var actingUserId = _actingUser.Get()?.Id;

        if (actingUserId.IsNotEmpty())
        {
            userHistory = await _practiceService.GetUserPracticeHistory(actingUserId, cancellationToken);
        }

        foreach (var result in results)
        {
            // hide tags which aren't in the "suggested searches" configured in the practice area
            // (this is because topo has lots of tags that aren't useful to players, so we only
            // want to show them values in the suggested search) 
            result.Tags = result.Tags.Where(t => sluggedSuggestedSearches.Contains(_slugger.Get(t)));

            // fix up relative urls
            result.Text = _challengeDocsService.ReplaceRelativeUris(result.Text);
        }

        // resolve paging arguments
        var pageSize = request.Filter.Take > 0 ? request.Filter.Take : 100;
        var pageNumber = request.Filter.Skip / pageSize;

        var pagedResults = _pagingService.Page(results, new PagingArgs
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        // append historical data
        if (userHistory.Length != 0)
        {
            foreach (var challenge in pagedResults.Items)
            {
                var challengeHistory = userHistory.FirstOrDefault(h => h.ChallengeSpecId == challenge.Id);

                challenge.UserBestAttempt = new PracticeChallengeViewUserHistory
                {
                    AttemptCount = challengeHistory?.AttemptCount ?? 0,
                    BestAttemptDate = challengeHistory?.BestAttemptDate ?? default,
                    BestAttemptScore = challengeHistory?.BestAttemptScore ?? default,
                    IsComplete = challengeHistory?.IsComplete ?? false
                };
            }
        }

        return new SearchPracticeChallengesResult { Results = pagedResults };
    }

    /// <summary>
    /// Load the transformed query results from the database. (Broken out into its own function for unit testing.)
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <param name="sluggedSuggestedSearches"></param>
    /// <returns></returns>
    internal async Task<IQueryable<Data.ChallengeSpec>> BuildQuery(string filterTerm, IEnumerable<string> sluggedSuggestedSearches)
    {
        var canViewHidden = await _permissionsService.Can(PermissionKey.Games_ViewUnpublished);

        var q = _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Game.PlayerMode == PlayerMode.Practice)
            .Where(s => !s.Disabled);

        if (!canViewHidden)
        {
            // without the permission, neither spec nor the game can be hidden
            q = q
                .Where(s => !s.IsHidden)
                .Where(s => s.Game.IsPublished);
        }

        if (filterTerm.IsNotEmpty())
        {
            q = q.Where(s => s.TextSearchVector.Matches(filterTerm) || s.Game.TextSearchVector.Matches(filterTerm));
            q = q.OrderByDescending(s => s.TextSearchVector.Rank(EF.Functions.PlainToTsQuery(filterTerm)))
                .ThenByDescending(s => s.Game.TextSearchVector.Rank(EF.Functions.PlainToTsQuery(filterTerm)))
                .ThenBy(s => s.Name);
        }
        else
        {
            q = q.OrderBy(s => s.Name);
        }

        return q;
    }
}
