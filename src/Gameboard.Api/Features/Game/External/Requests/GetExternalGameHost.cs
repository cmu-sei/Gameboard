using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed record GetExternalGameHostQuery(string Id) : IRequest<GetExternalGameHostsResponseHost>;

internal sealed class GetExternalGameHostHandler(
    IExternalGameHostService externalGameHostService,
    IValidatorService<GetExternalGameHostQuery> validator
    ) : IRequestHandler<GetExternalGameHostQuery, GetExternalGameHostsResponseHost>
{
    private readonly IExternalGameHostService _externalGameHostService = externalGameHostService;
    private readonly IValidatorService<GetExternalGameHostQuery> _validator = validator;

    public async Task<GetExternalGameHostsResponseHost> Handle(GetExternalGameHostQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .ConfigureAuthorization(config => config.RequirePermissions(PermissionKey.Games_AdminExternal))
            .Validate(request, cancellationToken);

        return await
            _externalGameHostService
            .GetHosts()
            .SingleAsync(h => h.Id == request.Id, cancellationToken);
    }
}
