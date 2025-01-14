using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record GetTeamsMailMetadataQuery(string GameId) : IRequest<IEnumerable<TeamSummary>>;

internal class GetTeamsMailMetadataHandler(PlayerService playerService, IValidatorService validatorService) : IRequestHandler<GetTeamsMailMetadataQuery, IEnumerable<TeamSummary>>
{
    private readonly PlayerService _playerService = playerService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<IEnumerable<TeamSummary>> Handle(GetTeamsMailMetadataQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.Require(PermissionKey.Teams_Observe))
            .Validate(cancellationToken);

        return await _playerService.LoadGameTeamsMailMetadata(request.GameId);
    }
}
