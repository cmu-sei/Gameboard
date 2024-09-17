using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record UpsertSupportSettingsAutoTagCommand(SupportSettingsAutoTag AutoTag) : IRequest<SupportSettingsAutoTag>;

internal class UpsertSupportSettingsAutoTagHandler(
    IGuidService guids,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<UpsertSupportSettingsAutoTagCommand, SupportSettingsAutoTag>
{
    private readonly IGuidService _guids = guids;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<SupportSettingsAutoTag> Handle(UpsertSupportSettingsAutoTagCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.RequirePermissions(Users.PermissionKey.Support_EditSettings))
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
            Description = request.AutoTag.Description,
            IsEnabled = request.AutoTag.IsEnabled,
            SupportSettingsId = settingsId,
            Tag = request.AutoTag.Tag,
        };

        var result = await _store.Create(newTag);
        return result;
    }
}
