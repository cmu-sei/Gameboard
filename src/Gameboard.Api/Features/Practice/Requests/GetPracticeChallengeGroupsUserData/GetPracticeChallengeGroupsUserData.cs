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

namespace Gameboard.Api.Features.Practice;

public record GetPracticeChallengeGroupsUserDataQuery(string UserId, string[] ChallengeGroupIds) : IRequest<GetPracticeChallengeGroupsUserDataResponse>;

internal sealed class GetUserChallengeGroupsHandler
(
    IActingUserService actingUserService,
    INowService nowService,
    IUserRolePermissionsService permissionsService,
    IPracticeService practiceService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetPracticeChallengeGroupsUserDataQuery, GetPracticeChallengeGroupsUserDataResponse>
{
    public async Task<GetPracticeChallengeGroupsUserDataResponse> Handle(GetPracticeChallengeGroupsUserDataQuery request, CancellationToken cancellationToken)
    {
        // the acting user could also be null, because we don't make them log in to see challenge collections
        var actingUser = actingUserService.Get();

        // the final resolved userId is either the one requested if it's specified, fall back to the logged in user if not, or ultimately null
        // because they might be unauthed
        var resolvedUserId = request.UserId.IsNotEmpty() ? request.UserId : actingUser?.Id;

        // but if there's no user, we don't have anything interesting to report, so bail
        if (resolvedUserId is null)
        {
            return new GetPracticeChallengeGroupsUserDataResponse { Groups = [] };
        }

        await validatorService
            // ANON OKAY, because practice, no .Auth call
            .AddEntityExistsValidator<Data.User>(resolvedUserId, false)
            .AddValidator(async ctx =>
            {
                // if we're pulling for a specific user, we need to either be that user or be someone with permission
                if (resolvedUserId == actingUser.Id)
                {
                    return;
                }

                if (!await permissionsService.Can(PermissionKey.Admin_View))
                {
                    ctx.AddValidationException(new CantAccessPracticeChallengeGroupsUserDataException(request.UserId));
                }
            })
            .Validate(cancellationToken);

        // one of the properties we need to compute is whether the user is eligible for a certificate
        // for any completed challenge. to do this, we need to know if there's a global cert for the practice area
        var practiceSettings = await practiceService.GetSettings(cancellationToken);
        var hasGlobalCertificate = practiceSettings.CertificateTemplateId.IsNotEmpty();

        // resolve the requested groups that exist
        // BTW: this is crazy gnarly because of the difficulty in translating a quasi-recursive query
        // to EF and the fact that either the parent or child or both could have challenges attached
        var flat = await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => request.ChallengeGroupIds.Contains(g.Id))
            .SelectMany(g =>
                // this group's challenges
                g.ChallengeSpecs.Select(s => new
                {
                    GroupId = g.Id,
                    GroupName = g.Name,
                    SpecId = s.Id,
                    SpecName = s.Name,
                    Points = s.Points,
                    HasCert = s.Game.PracticeCertificateTemplateId != null
                })
                // plus any challenges from child groups
                .Concat(
                    g
                        .ChildGroups
                        .SelectMany(c => c.ChallengeSpecs)
                        .Select(s => new
                        {
                            GroupId = g.Id,
                            GroupName = g.Name,
                            SpecId = s.Id,
                            SpecName = s.Name,
                            Points = s.Points,
                            HasCert = s.Game.PracticeCertificateTemplateId != null
                        })
                )
            )
            .ToArrayAsync(cancellationToken);

        // Group in memory
        var challengeGroups = flat
            .GroupBy(x => new { x.GroupId, x.GroupName })
            .Select(g => new
            {
                Id = g.Key.GroupId,
                Name = g.Key.GroupName,
                ChallengeCount = g.Count(),
                ChallengeMaxScoreTotal = g.Sum(x => x.Points),
                ChallengeSpecs = g.Select(s => new GetPracticeChallengeGroupsUserDataResponseChallenge
                {
                    Id = s.SpecId,
                    Name = s.SpecName,
                    HasCertificateTemplate = s.HasCert,
                    MaxPossibleScore = s.Points,
                    BestAttempt = null
                }).ToArray()
            })
            .ToArray();

        // shortcut out if there are no groups
        if (challengeGroups.Length == 0)
        {
            return new GetPracticeChallengeGroupsUserDataResponse { Groups = [] };
        }

        // compute user data
        var userGroupData = new Dictionary<string, GetPracticeChallengeGroupsUserDataResponseUserData>();
        var nowish = nowService.Get();
        var challengeSpecIds = challengeGroups.SelectMany(g => g.ChallengeSpecs.Select(s => s.Id)).ToArray();

        var challengeData = await store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Player.UserId == resolvedUserId)
            .Where(c => c.EndTime <= nowish)
            .Where(c => challengeSpecIds.Contains(c.SpecId))
            .GroupBy(c => c.SpecId)
            .Select(kv => new
            {
                SpecId = kv.Key,
                Challenge = kv.OrderByDescending(c => c.Score).Select(c => new
                {
                    IsComplete = c.Score >= c.Points,
                    c.StartTime,
                    c.Score,
                })
                .FirstOrDefault()
            })
            .ToDictionaryAsync(kv => kv.SpecId, kv => kv.Challenge, cancellationToken);

        foreach (var challengeGroup in challengeGroups)
        {
            userGroupData.Add(challengeGroup.Id, new GetPracticeChallengeGroupsUserDataResponseUserData() { ChallengesCompleteCount = 0, Score = 0 });

            foreach (var challengeSpec in challengeGroup.ChallengeSpecs)
            {
                if (challengeData.TryGetValue(challengeSpec.Id, out var bestAttempt))
                {
                    challengeSpec.BestAttempt = new GetPracticeChallengeGroupsUserDataChallengeAttempt
                    {
                        CertificateAwarded = bestAttempt.IsComplete && challengeSpec.HasCertificateTemplate,
                        Date = bestAttempt.StartTime,
                        Score = bestAttempt.Score
                    };

                    if (bestAttempt.IsComplete)
                    {
                        userGroupData[challengeGroup.Id].ChallengesCompleteCount += 1;
                    }

                    userGroupData[challengeGroup.Id].Score += bestAttempt.Score;
                }
            }
        }

        return new GetPracticeChallengeGroupsUserDataResponse
        {
            Groups = challengeGroups.Select(g => new GetPracticeChallengeGroupsUserDataResponseGroup
            {
                Id = g.Id,
                Name = g.Name,
                ChallengeCount = g.ChallengeCount,
                ChallengeMaxScoreTotal = g.ChallengeMaxScoreTotal,
                Challenges = g.ChallengeSpecs,
                UserData = userGroupData.GetValueOrDefault(g.Id)
            })
            .ToArray()
        };
    }
}
