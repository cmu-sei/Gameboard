using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record ListPracticeChallengeGroupsQuery(ListPracticeChallengeGroupsRequest Request) : IRequest<ListPracticeChallengeGroupsResponse>;

internal sealed class ListPracticeChallengeGroupsHandler
(
    IPracticeService practiceService,
    IValidatorService validatorService
) : IRequestHandler<ListPracticeChallengeGroupsQuery, ListPracticeChallengeGroupsResponse>
{
    public async Task<ListPracticeChallengeGroupsResponse> Handle(ListPracticeChallengeGroupsQuery request, CancellationToken cancellationToken)
    {
        await validatorService
            // ANON OKAY, because practice, no .Auth call
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.Request.ParentGroupId, false)
            .Validate(cancellationToken);

        var groups = await practiceService.ChallengeGroupsList(new ChallengeGroupsListArgs
        {
            ContainChallengeSpecId = request.Request.ContainChallengeSpecId,
            GetRootOnly = request.Request.GetRootOnly,
            ParentGroupId = request.Request.ParentGroupId,
            SearchTerm = request.Request.SearchTerm
        }, cancellationToken);

        return new ListPracticeChallengeGroupsResponse { Groups = groups };
    }
}
