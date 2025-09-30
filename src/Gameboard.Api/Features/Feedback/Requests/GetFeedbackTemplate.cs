// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record GetFeedbackTemplateQuery(string FeedbackTemplateId) : IRequest<FeedbackTemplateView>;

internal sealed class GetFeedbackTemplateHandler
(
    IMapper mapper,
    IStore store,
    EntityExistsValidator<FeedbackTemplate> templateExists,
    IValidatorService validatorService
) : IRequestHandler<GetFeedbackTemplateQuery, FeedbackTemplateView>
{
    private readonly IMapper _mapper = mapper;
    private readonly IStore _store = store;
    private readonly EntityExistsValidator<FeedbackTemplate> _templateExists = templateExists;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<FeedbackTemplateView> Handle(GetFeedbackTemplateQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .AddValidator(_templateExists.UseValue(request.FeedbackTemplateId))
            .Validate(cancellationToken);

        var query = _store
            .WithNoTracking<FeedbackTemplate>()
            .Where(t => t.Id == request.FeedbackTemplateId);

        return await _mapper
            .ProjectTo<FeedbackTemplateView>(query)
            .SingleAsync(cancellationToken);
    }
}
