// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record UpdateFeedbackTemplateCommand(UpdateFeedbackTemplateRequest Request) : IRequest<FeedbackTemplateView>;

internal sealed class UpdateFeedbackTemplateHandler
(
    IMapper mapper,
    IStore store,
    IValidatorService validator
) : IRequestHandler<UpdateFeedbackTemplateCommand, FeedbackTemplateView>
{
    private readonly IMapper _mapper = mapper;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<FeedbackTemplateView> Handle(UpdateFeedbackTemplateCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddEntityExistsValidator<FeedbackTemplate>(request.Request.Id)
            .AddValidator(request.Request.Name.IsEmpty(), new MissingRequiredInput<string>(nameof(request.Request.Name)))
            .AddValidator(request.Request.Content.IsEmpty(), new MissingRequiredInput<string>(nameof(request.Request.Content)))
            .AddValidator(async ctx =>
            {
                if (await _store.WithNoTracking<FeedbackTemplate>().AnyAsync(t => t.Name == request.Request.Name && t.Id != request.Request.Id))
                {
                    ctx.AddValidationException(new DuplicateFeedbackTemplateNameException(request.Request.Name));
                }
            })
            .Validate(cancellationToken);

        await _store
            .WithNoTracking<FeedbackTemplate>()
            .Where(t => t.Id == request.Request.Id)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(t => t.HelpText, request.Request.HelpText)
                    .SetProperty(t => t.Content, request.Request.Content)
                    .SetProperty(t => t.Name, request.Request.Name),
                cancellationToken
            );

        return await _mapper
            .ProjectTo<FeedbackTemplateView>(_store.WithNoTracking<FeedbackTemplate>().Where(t => t.Id == request.Request.Id))
            .SingleAsync(cancellationToken);
    }
}
