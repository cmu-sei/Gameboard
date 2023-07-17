using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record GetTeamQuery(string TeamId, User actingUser) : IRequest<Team>;

internal class GetTeamQueryHandler : IRequestHandler<GetTeamQuery, Team>
{
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GetTeamQuery> _validatorService;

    public GetTeamQueryHandler(ITeamService teamService, IValidatorService<GetTeamQuery> validatorService)
    {
        _teamService = teamService;
        _validatorService = validatorService;
    }

    public async Task<Team> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        return await _teamService.GetTeam(request.TeamId, cancellationToken);
    }
}
