using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPracticeCollectionsParameterOptionsQuery() : IRequest<SimpleEntity[]>;

internal sealed class GetPracticeCollectionsParameterOptionsHandler
(
    IStore store,
    IValidatorService validator
) : IRequestHandler<GetPracticeCollectionsParameterOptionsQuery, SimpleEntity[]>
{
    public async Task<SimpleEntity[]> Handle(GetPracticeCollectionsParameterOptionsQuery request, CancellationToken cancellationToken)
    {
        // validate
        await validator
            .Auth(c => c.Require(PermissionKey.Reports_View))
            .Validate(cancellationToken);

        return await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => g.ParentGroupId == null)
            .Select(g => new SimpleEntity
            {
                Id = g.Id,
                Name = g.Name
            })
            .OrderBy(g => g.Name)
            .ToArrayAsync(cancellationToken);
    }
}
