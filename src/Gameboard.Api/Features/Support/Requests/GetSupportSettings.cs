using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record GetSupportSettingsQuery() : IRequest<SupportSettingsViewModel>;

internal class GetSupportSettingsHandler : IRequestHandler<GetSupportSettingsQuery, SupportSettingsViewModel>
{
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetSupportSettingsHandler(IStore store, UserRoleAuthorizer userRoleAuthorizer)
    {
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<SupportSettingsViewModel> Handle(GetSupportSettingsQuery request, CancellationToken cancellationToken)
    {
        // validate
        _userRoleAuthorizer
            .AllowRoles(UserRole.Member)
            .Authorize();

        // provide a default if no one has created settings yet
        var existingSettings = await _store
            .WithNoTracking<SupportSettings>()
            .SingleOrDefaultAsync(cancellationToken);

        if (existingSettings is null)
            return new SupportSettingsViewModel { SupportPageGreeting = null };

        return new SupportSettingsViewModel
        {
            SupportPageGreeting = existingSettings.SupportPageGreeting
        };
    }
}
