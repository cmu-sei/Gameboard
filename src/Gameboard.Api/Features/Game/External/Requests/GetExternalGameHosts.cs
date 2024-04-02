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
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetExternalGameHostsHandler(IStore store, UserRoleAuthorizer userRoleAuthorizer)
    {
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetExternalGameHostsResponse> Handle(GetExternalGameHostsQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowAllElevatedRoles()
            .Authorize();

        var hosts = await _store
            .WithNoTracking<ExternalGameHost>()
            .Select(h => new GetExternalGameHostsResponseHost
            {
                Id = h.Id,
                Name = h.Name,
                ClientUrl = h.ClientUrl,
                DestroyResourcesOnDeployFailure = h.DestroyResourcesOnDeployFailure,
                GamespaceDeployBatchSize = h.GamespaceDeployBatchSize,
                HostApiKey = h.HostApiKey,
                HostUrl = h.HostUrl,
                PingEndpoint = h.PingEndpoint,
                StartupEndpoint = h.StartupEndpoint,
                TeamExtendedEndpoint = h.TeamExtendedEndpoint,
                UsedByGames = h.UsedByGames.Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            })
            .ToArrayAsync(cancellationToken);

        return new GetExternalGameHostsResponse { Hosts = hosts };
    }
}
