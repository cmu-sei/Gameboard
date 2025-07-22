using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeChallengeGroupQuery(string Id) : IRequest<GetPracticeChallengeGroupResponse>;

internal sealed class GetPracticeChallengeGroupHandler
(
    IStore store,
    IValidatorService validator
) : IRequestHandler<GetPracticeChallengeGroupQuery, GetPracticeChallengeGroupResponse>
{
    public async Task<GetPracticeChallengeGroupResponse> Handle(GetPracticeChallengeGroupQuery request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.Id)
            .Validate(cancellationToken);

        var group = await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => g.Id == request.Id)
            .Select(g => new GetPracticeChallengeGroupResponse
            {
                Group = new GetPracticeChallengeGroupResponseGroup
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    ImageUrl = g.ImageUrl,
                    IsFeatured = g.IsFeatured,
                    Challenges = g.ChallengeSpecs.Select(s => new GetPracticeChallengeGroupResponseChallenge
                    {
                        Id = s.Id,
                        Name = s.Name,
                        CountCompleted = 0,
                        CountLaunched = 0,
                        LastLaunched = null,
                    })
                    .OrderBy(c => c.Name)
                    .ToArray(),
                },
                ParentGroup = g.ParentGroup == null ? null : new SimpleEntity
                {
                    Id = g.ParentGroup.Id,
                    Name = g.ParentGroup.Name,
                },
                ChildGroups = g.ChildGroups.Select(c => new GetPracticeChallengeGroupResponseGroup
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsFeatured = c.IsFeatured,
                    Challenges = c.ChallengeSpecs.Select(s => new GetPracticeChallengeGroupResponseChallenge
                    {
                        Id = s.Id,
                        Name = s.Name,
                        CountCompleted = 0,
                        CountLaunched = 0,
                        LastLaunched = null
                    })
                    .ToArray()
                })
                .OrderBy(g => g.IsFeatured ? 0 : 1)
                    .ThenBy(g => g.Name)
                .ToArray()
            })
            .SingleAsync(cancellationToken);

        // we have to load the challenge data separately because #just317things
        // https://github.com/cmu-sei/Gameboard/issues/317
        var allChallenges = new List<GetPracticeChallengeGroupResponseChallenge>(group.Group.Challenges);
        allChallenges.AddRange(group.ChildGroups.SelectMany(g => g.Challenges));
        var challengeSpecIds = allChallenges.Select(c => c.Id).Distinct().ToArray();

        // load launch data for these
        var challengeSpecData = await store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeSpecIds.Contains(c.SpecId))
            .Where(c => c.PlayerMode == PlayerMode.Practice)
            .Select(c => new
            {
                c.Id,
                c.StartTime,
                c.SpecId,
                Completed = c.Score >= c.Points
            })
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(gr => gr.Key, gr => new
            {
                CountLaunched = gr.Count(),
                CountCompleted = gr.Where(c => c.Completed).Count(),
                LastLaunched = gr.Max(t => t.StartTime)
            }, cancellationToken);

        // fill out launch data for group and childgroups
        foreach (var spec in group.Group.Challenges)
        {
            if (challengeSpecData.TryGetValue(spec.Id, out var challengeData))
            {
                spec.CountCompleted = challengeData.CountCompleted;
                spec.CountLaunched = challengeData.CountLaunched;
                spec.LastLaunched = challengeData.LastLaunched == DateTimeOffset.MinValue ? null : challengeData.LastLaunched;
            }
        }

        foreach (var childGroup in group.ChildGroups)
        {
            foreach (var spec in childGroup.Challenges)
            {
                if (challengeSpecData.TryGetValue(spec.Id, out var challengeData))
                {
                    spec.CountCompleted = challengeData.CountCompleted;
                    spec.CountLaunched = challengeData.CountLaunched;
                    spec.LastLaunched = challengeData.LastLaunched == DateTimeOffset.MinValue ? null : challengeData.LastLaunched;
                }
            }
        }

        return group;
    }
}
