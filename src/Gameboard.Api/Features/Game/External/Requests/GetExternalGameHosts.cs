using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed class GetExternalGameHostsResponse
{
    public required IEnumerable<GetExternalGameHostsResponseHost> Hosts { get; set; }
}

public sealed record GetExternalGameHostsQuery() : IRequest<GetExternalGameHostsResponse>;

internal sealed class GetExternalGameHostsHandler(
    IExternalGameHostService externalGameHostService,
    IValidatorService validatorService
    ) : IRequestHandler<GetExternalGameHostsQuery, GetExternalGameHostsResponse>
{
    private readonly IExternalGameHostService _externalGameHostService = externalGameHostService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetExternalGameHostsResponse> Handle(GetExternalGameHostsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(config => config.RequirePermissions(Users.PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        return new GetExternalGameHostsResponse
        {
            Hosts = await _externalGameHostService
                .GetHosts()
                .OrderBy(h => h.Name)
                    .ThenBy(h => h.HostUrl)
                .ToArrayAsync(cancellationToken)
        };
    }
}
