// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record UpsertSupportSettingsAutoTagCommand(UpsertSupportSettingsAutoTagRequest AutoTag) : IRequest<SupportSettingsAutoTag>;

internal class UpsertSupportSettingsAutoTagHandler(
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<UpsertSupportSettingsAutoTagCommand, SupportSettingsAutoTag>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<SupportSettingsAutoTag> Handle(UpsertSupportSettingsAutoTagCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.Require(Users.PermissionKey.Support_EditSettings))
            .AddValidator(ctx =>
            {
                if (request.AutoTag.ConditionValue.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.AutoTag.ConditionValue), request.AutoTag.ConditionValue));
                if (request.AutoTag.Tag.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.AutoTag.Tag), request.AutoTag.Tag));
            })
            .Validate(cancellationToken);

        if (request.AutoTag.Id.IsNotEmpty())
        {
            await _store
                .WithNoTracking<SupportSettingsAutoTag>()
                .Where(t => t.Id == request.AutoTag.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var settingsId = await _store
            .WithTracking<SupportSettings>()
            .Select(s => s.Id)
            .SingleAsync(cancellationToken);

        var newTag = new SupportSettingsAutoTag
        {
            ConditionType = request.AutoTag.ConditionType,
            ConditionValue = request.AutoTag.ConditionValue,
            IsEnabled = request.AutoTag.IsEnabled ?? true,
            SupportSettingsId = settingsId,
            Tag = request.AutoTag.Tag,
        };

        var result = await _store.Create(newTag);
        return result;
    }
}
