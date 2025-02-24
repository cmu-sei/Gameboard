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

        var allTags = allPracticeChallenges.SelectMany(c => challengesService.GetTags(c.Tags)).Distinct();
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

            // add/increment all tag counts
            foreach (var tag in challengesService.GetTags(challenge.Tags))
            {
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
                    engagement.CountAttempted += 1;
                    engagement.PointsScored += userChallengeHistory.BestAttemptScore ?? 0;
                    totalPointsScored += userChallengeHistory?.BestAttemptScore ?? 0;

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
            Tags = [.. tagEngagement.OrderByDescending(kv => kv.Value.CountCompleted).Select(kv => kv.Value)]
        };
    }
}
