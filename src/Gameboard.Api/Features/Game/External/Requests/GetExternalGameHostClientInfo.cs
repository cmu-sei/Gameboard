using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed record GetExternalGameHostClientInfoQuery(string HostId) : IRequest<GetExternalGameHostClientInfo>;

internal sealed class GetExternalGameHostClientInfoHandler(IStore store, IValidatorService validatorService) : IRequestHandler<GetExternalGameHostClientInfoQuery, GetExternalGameHostClientInfo>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetExternalGameHostClientInfo> Handle(GetExternalGameHostClientInfoQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config => config.RequireAuthentication())
            .Validate(cancellationToken);

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
