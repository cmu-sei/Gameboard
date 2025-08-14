using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record UpdatePracticeChallengeGroupCommand(UpdatePracticeChallengeGroupRequest Request) : IRequest<PracticeChallengeGroupDto>;

internal sealed class UpdatePracticeChallengeGroupHandler
(
    IActingUserService actingUserService,
    IImageStoreService imageStore,
    INowService now,
    IPracticeService practiceService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<UpdatePracticeChallengeGroupCommand, PracticeChallengeGroupDto>
{
    public async Task<PracticeChallengeGroupDto> Handle(UpdatePracticeChallengeGroupCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddValidator(async ctx =>
            {
                // if parent group id is specified, special validation applies
                if (request.Request.ParentGroupId.IsEmpty())
                {
                    return;
                }

                // it must exist and not have a parent of its own
                var parentGroupValid = await store
                    .WithNoTracking<PracticeChallengeGroup>()
                    .Where(g => g.Id == request.Request.ParentGroupId)
                    .Select(g => new
                    {
                        g.Id,
                        g.ParentGroupId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (parentGroupValid is null)
                {
                    ctx.AddValidationException(new ResourceNotFound<PracticeChallengeGroup>(request.Request.ParentGroupId));
                }
                else if (parentGroupValid.ParentGroupId.IsNotEmpty())
                {
                    ctx.AddValidationException(new ChallengeGroupInvalidParentException(request.Request.ParentGroupId, parentGroupValid.ParentGroupId));
                }
            })
            .AddValidator(async ctx =>
            {
                // the group being updated must exist, and if it has children, it can't have a parent
                var updatingGroup = await store
                    .WithNoTracking<PracticeChallengeGroup>()
                    .Where(g => g.Id == request.Request.Id)
                    .Select(g => new
                    {
                        g.Id,
                        ChildGroupsCount = g.ChildGroups.Count
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (updatingGroup is null)
                {
                    ctx.AddValidationException(new ResourceNotFound<PracticeChallengeGroup>(request.Request.Id));
                }
                else if (updatingGroup.ChildGroupsCount > 0 && request.Request.ParentGroupId.IsNotEmpty())
                {
                    ctx.AddValidationException(new ChallengeGroupInvalidUpdateBecauseParentsAndKidsException(request.Request.Id, request.Request.ParentGroupId, updatingGroup.ChildGroupsCount));
                }
            })
            .AddValidator(ctx =>
            {
                if (request.Request.Name.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(CreatePracticeChallengeGroupRequest.Name)));
                }

                if (request.Request.Description.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(CreatePracticeChallengeGroupRequest.Description)));
                }
            })
            .Validate(cancellationToken);


        // update the image if provided (otherwise, it'll be implicitly removed since this defaults to empty)
        var newImageUrl = string.Empty;
        if (request.Request.Image != null)
        {
            // we're intentionally not sweating cleaning up old images for now
            newImageUrl = await imageStore.SaveImage(request.Request.Image, ImageStoreType.PracticeChallengeGroup, request.Request.Id, cancellationToken);
        }
        else
        {
            if (request.Request.RemoveImage)
            {
                newImageUrl = null;
            }
            else
            {
                // we just read the existing image out of the DB because it makes the below update clearer/easier
                newImageUrl = await store
                    .WithNoTracking<PracticeChallengeGroup>()
                    .Where(g => g.Id == request.Request.Id)
                    .Select(g => g.ImageUrl)
                    .SingleOrDefaultAsync(cancellationToken);
            }

        }

        var actingUser = actingUserService.Get();
        await store.WithNoTracking<PracticeChallengeGroup>()
            .Where(g => g.Id == request.Request.Id)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(g => g.Name, request.Request.Name)
                    .SetProperty(g => g.Description, request.Request.Description)
                    .SetProperty(g => g.ImageUrl, g => newImageUrl)
                    .SetProperty(g => g.IsFeatured, request.Request.IsFeatured)
                    .SetProperty(g => g.ParentGroupId, request.Request.ParentGroupId)
                    .SetProperty(g => g.UpdatedByUserId, actingUser.Id)
                    .SetProperty(g => g.UpdatedOn, now.Get()),
                cancellationToken
            );

        return await practiceService.ChallengeGroupGet(request.Request.Id, cancellationToken);
    }
}
