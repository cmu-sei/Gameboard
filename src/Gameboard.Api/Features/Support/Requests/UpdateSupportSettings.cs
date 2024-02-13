using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record UpdateSupportSettingsCommand(SupportSettingsViewModel Settings) : IRequest<SupportSettingsViewModel>;

internal class UpdateSupportSettingsHandler : IRequestHandler<UpdateSupportSettingsCommand, SupportSettingsViewModel>
{
    private readonly IActingUserService _actingUserService;
    private readonly INowService _nowService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public UpdateSupportSettingsHandler
    (
        IActingUserService actingUserService,
        INowService nowService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _actingUserService = actingUserService;
        _nowService = nowService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<SupportSettingsViewModel> Handle(UpdateSupportSettingsCommand request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

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
