using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed class GetExternalGameHostsResponse
{
    public required IEnumerable<GetExternalGameHostsResponseHost> Hosts { get; set; }
}

public sealed record GetExternalGameHostsQuery() : IRequest<GetExternalGameHostsResponse>;

internal sealed class GetExternalGameHostsHandler : IRequestHandler<GetExternalGameHostsQuery, GetExternalGameHostsResponse>
{
    private readonly IExternalGameService _externalGameService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetExternalGameHostsHandler
    (
        IExternalGameService externalGameService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _externalGameService = externalGameService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetExternalGameHostsResponse> Handle(GetExternalGameHostsQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowAllElevatedRoles()
            .Authorize();

        return new GetExternalGameHostsResponse
        {
            Hosts = await _externalGameService
                .GetHosts()
                .ToArrayAsync(cancellationToken)
        };
    }
}
