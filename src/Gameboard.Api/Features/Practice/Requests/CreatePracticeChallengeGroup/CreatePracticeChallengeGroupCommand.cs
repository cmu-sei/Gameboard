// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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

public record CreatePracticeChallengeGroupCommand(CreatePracticeChallengeGroupRequest Request) : IRequest<PracticeChallengeGroupDto>;

internal sealed class CreatePracticeChallengeGroupHandler
(
    IActingUserService actingUserService,
    IImageStoreService imageStore,
    INowService now,
    IPracticeService practiceService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<CreatePracticeChallengeGroupCommand, PracticeChallengeGroupDto>
{
    public async Task<PracticeChallengeGroupDto> Handle(CreatePracticeChallengeGroupCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Practice_EditSettings))
            .AddValidator(async ctx =>
            {
                if (request.Request.ParentGroupId.IsEmpty())
                {
                    return;
                }

                // if parent group id is specified, it must exist and not have a parent of its own
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

        var actingUser = actingUserService.Get();
        var newGroup = new PracticeChallengeGroup
        {
            Name = request.Request.Name,
            Description = request.Request.Description,
            ParentGroupId = request.Request.ParentGroupId.IsNotEmpty() ? request.Request.ParentGroupId : null,
            ChallengeSpecs = [],
            IsFeatured = request.Request.IsFeatured,
            CreatedOn = now.Get(),
            CreatedByUserId = actingUser.Id
        };

        var createdGroup = await store.Create(newGroup);
        var imageUrl = "";

        // if image passed, save it too
        if (request.Request.Image != null)
        {
            imageUrl = await imageStore.SaveImage(request.Request.Image, ImageStoreType.PracticeChallengeGroup, createdGroup.Id, cancellationToken);

            await store
                .WithNoTracking<PracticeChallengeGroup>()
                .Where(g => g.Id == createdGroup.Id)
                .ExecuteUpdateAsync(up => up.SetProperty(g => g.ImageUrl, imageUrl), cancellationToken);
        }

        return await practiceService.ChallengeGroupGet(createdGroup.Id, cancellationToken);
    }
}
