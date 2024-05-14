using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed record GetExternalGameHostClientInfoQuery(string HostId) : IRequest<GetExternalGameHostClientInfo>;

internal sealed class GetExternalGameHostClientInfoHandler : IRequestHandler<GetExternalGameHostClientInfoQuery, GetExternalGameHostClientInfo>
{
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuth;

    public GetExternalGameHostClientInfoHandler(IStore store, UserRoleAuthorizer userRoleAuth)
    {
        _store = store;
        _userRoleAuth = userRoleAuth;
    }

    public async Task<GetExternalGameHostClientInfo> Handle(GetExternalGameHostClientInfoQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuth
            .AllowRoles(UserRole.Member)
            .Authorize();

        return await _store
            .WithNoTracking<ExternalGameHost>()
            .Select(h => new GetExternalGameHostClientInfo
            {
                Id = h.Id,
                Name = h.Name,
                ClientUrl = h.ClientUrl
            })
            .SingleAsync(h => h.Id == request.HostId, cancellationToken);
    }
}
