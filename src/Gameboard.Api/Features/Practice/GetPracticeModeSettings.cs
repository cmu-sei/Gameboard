using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeSettingsQuery(User ActingUser) : IRequest<PracticeModeSettings>;

internal class GetPracticeModeSettingsHandler : IRequestHandler<GetPracticeModeSettingsQuery, PracticeModeSettings>
{
    private readonly UserRoleAuthorizer _roleAuthorizer;
    private readonly IStore _store;

    public GetPracticeModeSettingsHandler(UserRoleAuthorizer roleAuthorizer, IStore store)
    {
        _roleAuthorizer = roleAuthorizer;
        _store = store;
    }

    public Task<PracticeModeSettings> Handle(GetPracticeModeSettingsQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin };
        _roleAuthorizer.Authorize();

        return _store.FirstOrDefaultAsync<Data.PracticeModeSettings>(cancellationToken);
    }
}
