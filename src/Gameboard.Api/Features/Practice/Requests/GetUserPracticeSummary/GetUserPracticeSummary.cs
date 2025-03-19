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

namespace Gameboard.Api.Features.Practice.Requests;

public record GetUserPracticeSummaryRequest(string UserId) : IRequest<GetUserPracticeSummaryResponse>;

internal sealed class GetUserPracticeSummaryHandler
(
    ChallengeService challengesService,
    IPracticeService practiceService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetUserPracticeSummaryRequest, GetUserPracticeSummaryResponse>
{
    public async Task<GetUserPracticeSummaryResponse> Handle(GetUserPracticeSummaryRequest request, CancellationToken cancellationToken)
    {
        await validatorService
            .Auth(c => c.Require(PermissionKey.Admin_View).UnlessUserIdIn(request.UserId))
            .Validate(cancellationToken);

        var userHistory = await practiceService.GetUserPracticeHistory(request.UserId, cancellationToken);
        var practiceSettings = await practiceService.GetSettings(cancellationToken);
        var hasGlobalTemplate = practiceSettings.CertificateTemplateId.IsNotEmpty();

        var allPracticeChallenges = await store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(c => !c.IsHidden && !c.Disabled && c.Game.IsPublished)
            .Where(c => c.Game.PlayerMode == PlayerMode.Practice)
            .Select(c => new
            {
                c.Id,
                c.Tags,
                c.Points,
                HasCertificate = practiceSettings.CertificateTemplateId
            })
            .ToArrayAsync(cancellationToken);

        var tagEngagement = new Dictionary<string, UserPracticeSummaryResponseTagEngagement>();
        var countAttempted = 0;
        var countCompleted = 0;
        var totalPointsAvailable = 0d;
        var totalPointsScored = 0d;

        foreach (var challenge in allPracticeChallenges)
        {
            // add the total available points 
            totalPointsAvailable += challenge.Points;

            // has the user tried this one?
            var userChallengeHistory = userHistory.SingleOrDefault(c => c.ChallengeSpecId == challenge.Id);

            // update total points scored if played
            totalPointsScored += userChallengeHistory?.BestAttemptScore ?? 0;

            // add/increment all tag counts
            foreach (var tag in challengesService.GetTags(challenge.Tags))
            {
                if (!practiceSettings.SuggestedSearches.Contains(tag))
                {
                    continue;
                }

                if (!tagEngagement.TryGetValue(tag, out var engagement))
                {
                    engagement = new UserPracticeSummaryResponseTagEngagement
                    {
                        Tag = tag,
                        CountAttempted = 0,
                        CountAvailable = 0,
                        CountCompleted = 0,
                        PointsAvailable = 0,
                        PointsScored = 0
                    };

                    tagEngagement.Add(tag, engagement);
                }

                engagement.CountAvailable += 1;
                engagement.PointsAvailable += challenge.Points;

                if (userChallengeHistory is not null)
                {
                    countAttempted += 1;
                    engagement.CountAttempted += 1;
                    engagement.PointsScored += userChallengeHistory.BestAttemptScore ?? 0;

                    if (userChallengeHistory.IsComplete)
                    {
                        countCompleted += 1;
                        engagement.CountCompleted += 1;
                    }
                }
            }
        }

        return new GetUserPracticeSummaryResponse
        {
            CountAttempted = countAttempted,
            CountAvailable = allPracticeChallenges.Length,
            CountCompleted = countCompleted,
            PointsAvailable = totalPointsAvailable,
            PointsScored = totalPointsScored,
            TagsPlayed = [..
                tagEngagement
                    .OrderByDescending(kv => kv.Value.CountCompleted)
                    .Select(kv => kv.Value)
                    .Where(kv => kv.CountAttempted > 0)
            ],
            TagsUnplayed = [..
                allPracticeChallenges
                    .SelectMany(c => challengesService.GetTags(c.Tags))
                    .Where(t => practiceSettings.SuggestedSearches.Contains(t))
                    .Where(t => !tagEngagement.ContainsKey(t) || tagEngagement[t].CountAttempted == 0)
            ]
        };
    }
}
