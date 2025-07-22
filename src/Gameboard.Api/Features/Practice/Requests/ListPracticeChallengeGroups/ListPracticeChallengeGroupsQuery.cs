using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record ListPracticeChallengeGroupsQuery(ListPracticeChallengeGroupsRequest Request) : IRequest<ListPracticeChallengeGroupsResponse>;

internal sealed class ListPracticeChallengeGroupsHandler
(
    IStore store
) : IRequestHandler<ListPracticeChallengeGroupsQuery, ListPracticeChallengeGroupsResponse>
{
    public async Task<ListPracticeChallengeGroupsResponse> Handle(ListPracticeChallengeGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => g.ParentGroupId == null)
            .Select(g => new ListPracticeChallengeGroupsResponseGroup
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                ChallengeCount = g.ChallengeSpecs.Count + g.ChildGroups.Select(g => g.ChallengeSpecs.Count).Sum(),
                ImageUrl = g.ImageUrl,
                IsFeatured = g.IsFeatured,
                ParentGroupId = g.ParentGroupId,
                ChildGroups = g.ChildGroups.Select(c => new ListPracticeChallengeGroupsResponseGroup
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ChallengeCount = c.ChallengeSpecs.Count,
                    ImageUrl = c.ImageUrl,
                    IsFeatured = c.IsFeatured,
                    ParentGroupId = c.ParentGroupId
                }).ToArray()
            })
            .OrderBy(g => g.IsFeatured ? 0 : 1)
                .ThenBy(g => g.Name)
            .ToArrayAsync(cancellationToken);

        return new ListPracticeChallengeGroupsResponse { Groups = groups };
    }
}
