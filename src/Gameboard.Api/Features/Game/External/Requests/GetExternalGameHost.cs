using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed record GetExternalGameHostQuery(string Id) : IRequest<GetExternalGameHostsResponseHost>;

internal sealed class GetExternalGameHostHandler : IRequestHandler<GetExternalGameHostQuery, GetExternalGameHostsResponseHost>
{
    private readonly IExternalGameHostService _externalGameHostService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetExternalGameHostQuery> _validator;

    public GetExternalGameHostHandler
    (
        IExternalGameHostService externalGameHostService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetExternalGameHostQuery> validator
    )
    {
        _externalGameHostService = externalGameHostService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<GetExternalGameHostsResponseHost> Handle(GetExternalGameHostQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowAllElevatedRoles()
            .Authorize();

        return await
            _externalGameHostService
            .GetHosts()
            .SingleAsync(h => h.Id == request.Id, cancellationToken);
    }
}
