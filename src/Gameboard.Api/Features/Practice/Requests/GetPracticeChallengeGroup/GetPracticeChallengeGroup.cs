using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeChallengeGroupQuery(string Id) : IRequest<GetPracticeChallengeGroupResponse>;

internal sealed class GetPracticeChallengeGroupHandler
(
    IPracticeService practiceService,
    IValidatorService validator
) : IRequestHandler<GetPracticeChallengeGroupQuery, GetPracticeChallengeGroupResponse>
{
    public async Task<GetPracticeChallengeGroupResponse> Handle(GetPracticeChallengeGroupQuery request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.Id)
            .Validate(cancellationToken);

        var group = await practiceService.ChallengeGroupGet(request.Id, cancellationToken);
        return new GetPracticeChallengeGroupResponse { Group = group };
    }
}
