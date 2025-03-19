using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record SearchPracticeChallengesQuery(SearchFilter Filter, SearchPracticeChallengesRequestUserProgress? UserProgress = default) : IRequest<SearchPracticeChallengesResult>;

internal class SearchPracticeChallengesHandler
(
    IActingUserService actingUser,
    IChallengeDocsService challengeDocsService,
    IPagingService pagingService,
    IPracticeService practiceService,
    ISlugService slugger
) : IRequestHandler<SearchPracticeChallengesQuery, SearchPracticeChallengesResult>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly IChallengeDocsService _challengeDocsService = challengeDocsService;
    private readonly IPagingService _pagingService = pagingService;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly ISlugService _slugger = slugger;

    public async Task<SearchPracticeChallengesResult> Handle(SearchPracticeChallengesQuery request, CancellationToken cancellationToken)
    {
        // load settings - we need these to make decisions about tag-based matches
        var settings = await _practiceService.GetSettings(cancellationToken);
        var sluggedSuggestedSearches = settings.SuggestedSearches.Select(_slugger.Get);
        var hasGlobalPracticeCertificate = settings.CertificateTemplateId.IsNotEmpty();

        var query = await _practiceService.GetPracticeChallengesQueryBase(request.Filter.Term);
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

        // append historical data
        if (userHistory.Length != 0)
        {
            foreach (var challenge in results)
            {
                var challengeHistory = userHistory.FirstOrDefault(h => h.ChallengeSpecId == challenge.Id);

                challenge.UserBestAttempt = new PracticeChallengeViewUserHistory
                {
                    AttemptCount = challengeHistory?.AttemptCount ?? 0,
                    BestAttemptDate = challengeHistory?.BestAttemptDate ?? default(DateTimeOffset?),
                    BestAttemptScore = challengeHistory?.BestAttemptScore ?? default(double?),
                    IsComplete = challengeHistory?.IsComplete ?? false
                };
            }
        }

        // filter by complete if requested
        if (request.UserProgress.HasValue)
        {
            switch (request.UserProgress.Value)
            {
                case SearchPracticeChallengesRequestUserProgress.NotAttempted:
                    results = [.. results.Where(c => c.UserBestAttempt?.BestAttemptDate == default)];
                    break;
                case SearchPracticeChallengesRequestUserProgress.Attempted:
                    results = [.. results.Where(c => c.UserBestAttempt?.BestAttemptDate != default && !c.UserBestAttempt.IsComplete)];
                    break;
                case SearchPracticeChallengesRequestUserProgress.Completed:
                    results = [.. results.Where(c => c.UserBestAttempt is not null && c.UserBestAttempt.IsComplete)];
                    break;
            }
        }

        // resolve paging arguments
        var pageSize = request.Filter.Take > 0 ? request.Filter.Take : 100;
        var pageNumber = request.Filter.Skip / pageSize;

        var pagedResults = _pagingService.Page(results, new PagingArgs
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return new SearchPracticeChallengesResult { Results = pagedResults };
    }
}
