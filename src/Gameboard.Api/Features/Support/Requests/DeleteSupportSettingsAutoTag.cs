// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public sealed record DeleteSupportSettingsAutoTagCommand(string Id) : IRequest;

internal sealed class DeleteSupportSettingsAutoTagHandler(
    IStore store,
    EntityExistsValidator<SupportSettingsAutoTag> tagExists,
    IValidatorService validatorService) : IRequestHandler<DeleteSupportSettingsAutoTagCommand>
{
    private readonly IStore _store = store;
    private readonly EntityExistsValidator<SupportSettingsAutoTag> _tagExists = tagExists;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task Handle(DeleteSupportSettingsAutoTagCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.Require(Users.PermissionKey.Support_EditSettings))
            .AddValidator(_tagExists.UseValue(request.Id))
            .Validate(cancellationToken);

        await _store
            .WithNoTracking<SupportSettingsAutoTag>()
            .Where(t => t.Id == request.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
