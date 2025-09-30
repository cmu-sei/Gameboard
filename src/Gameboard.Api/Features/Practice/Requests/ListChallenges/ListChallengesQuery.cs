// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record ListChallengesQuery(ListChallengesRequest Request) : IRequest<PracticeChallengeView[]>;

internal sealed class ListChallengesHandler
(
    IPracticeService practiceService,
    IValidatorService validatorService
) : IRequestHandler<ListChallengesQuery, PracticeChallengeView[]>
{
    public async Task<PracticeChallengeView[]> Handle(ListChallengesQuery request, CancellationToken cancellationToken)
    {
        await validatorService
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .Validate(cancellationToken);

        var query = await practiceService.GetPracticeChallengesQueryBase(request.Request.SearchTerm);
        var results = await query
            .Select(s => new PracticeChallengeView
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Text = s.Text,
                AverageDeploySeconds = s.AverageDeploySeconds,
                HasCertificateTemplate = s.Game.PracticeCertificateTemplateId != null,
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
            .OrderBy(s => s.Name)
                .ThenBy(s => s.Game.Name)
            .ToArrayAsync(cancellationToken);

        return results;
    }
}
