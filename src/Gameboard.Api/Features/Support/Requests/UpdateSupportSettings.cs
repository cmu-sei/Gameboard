using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record UpdateSupportSettingsCommand(SupportSettingsViewModel Settings) : IRequest<SupportSettingsViewModel>;

internal class UpdateSupportSettingsHandler(
    IActingUserService actingUserService,
    INowService nowService,
    IStore store,
    IValidatorService validatorService
    ) : IRequestHandler<UpdateSupportSettingsCommand, SupportSettingsViewModel>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly INowService _nowService = nowService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<SupportSettingsViewModel> Handle(UpdateSupportSettingsCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(UserRolePermissionKey.Support_EditSettings))
            .Validate(cancellationToken);

        var existingSettings = await _store
            .WithTracking<SupportSettings>()
            .SingleOrDefaultAsync(cancellationToken);

        if (existingSettings is null)
        {
            await _store
                .Create(new SupportSettings
                {
                    SupportPageGreeting = request.Settings.SupportPageGreeting,
                    UpdatedByUserId = _actingUserService.Get().Id,
                    UpdatedOn = _nowService.Get()
                });
        }
        else
        {
            existingSettings.SupportPageGreeting = request.Settings.SupportPageGreeting;
            existingSettings.UpdatedByUserId = _actingUserService.Get().Id;
            existingSettings.UpdatedOn = _nowService.Get();
            await _store.SaveUpdate(existingSettings, cancellationToken);
        }

        return new SupportSettingsViewModel { SupportPageGreeting = request.Settings.SupportPageGreeting };
    }
}
