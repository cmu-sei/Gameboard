using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record DeletePracticeChallengeGroupCommand(string Id) : IRequest;

internal sealed class DeletePracticeChallengeGroupHandler(IStore store, IValidatorService validator) : IRequestHandler<DeletePracticeChallengeGroupCommand>
{
    public async Task Handle(DeletePracticeChallengeGroupCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.Id)
            .Validate(cancellationToken);

        // delete both this group and any child groups
        await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => g.Id == request.Id || g.ParentGroupId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
