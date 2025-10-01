// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
            .Auth(config => config.Require(PermissionKey.Games_CreateEditDelete))
            .Validate(request, cancellationToken);

        return await
            _externalGameHostService
            .GetHosts()
            .SingleAsync(h => h.Id == request.Id, cancellationToken);
    }
}
