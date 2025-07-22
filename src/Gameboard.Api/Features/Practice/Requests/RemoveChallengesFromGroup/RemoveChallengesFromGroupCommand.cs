using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record RemoveChallengesFromGroupCommand(string PracticeChallengeGroupId, string[] ChallengeSpecIds) : IRequest;

internal sealed class RemoveChallengesFromGroupHandler
(
    IStore store,
    IValidatorService validator
) : IRequestHandler<RemoveChallengesFromGroupCommand>
{
    public async Task Handle(RemoveChallengesFromGroupCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddEntityExistsValidator<PracticeChallengeGroup>(request.PracticeChallengeGroupId)
            .AddValidator(ctx =>
            {
                if (request.ChallengeSpecIds is null || request.ChallengeSpecIds.Length == 0)
                {
                    ctx.AddValidationException(new MissingRequiredInput<string[]>(nameof(request.ChallengeSpecIds)));
                }
            })
            .AddValidator(async ctx =>
            {
                var distinctIds = request.ChallengeSpecIds.Distinct().ToArray();
                var existingIds = await store
                    .WithNoTracking<Data.ChallengeSpec>()
                    .Where(s => request.ChallengeSpecIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToArrayAsync(cancellationToken);

                var nonExistentIds = distinctIds.Where(id => !existingIds.Contains(id)).ToArray();
                foreach (var id in nonExistentIds)
                {
                    ctx.AddValidationException(new ResourceNotFound<Data.ChallengeSpec>(id));
                }
            })
            .Validate(cancellationToken);

        await store.DoTransaction(async ctx =>
        {
            var specs = request.ChallengeSpecIds.Select(sId => new Data.ChallengeSpec { Id = sId }).ToArray();
            var group = await ctx.PracticeChallengeGroups.Include(g => g.ChallengeSpecs).SingleAsync(g => g.Id == request.PracticeChallengeGroupId, cancellationToken);

            foreach (var specId in request.ChallengeSpecIds)
            {
                var spec = group.ChallengeSpecs.SingleOrDefault(s => s.Id == specId);

                if (spec != null)
                {
                    group.ChallengeSpecs.Remove(spec);
                }
            }

            // foreach (var spec in specs)
            // {
            //     spec = group.ChallengeSpecs.Single(s => s.Id == spec.Id)
            //         group.ChallengeSpecs.Remove(spec);
            // }

            await ctx.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }
}
