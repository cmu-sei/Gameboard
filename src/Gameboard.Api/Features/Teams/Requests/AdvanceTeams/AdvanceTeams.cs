using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record AdvanceTeamsCommand(string GameId, bool IncludeScores, IEnumerable<string> TeamIds) : IRequest;

internal sealed class AdvanceTeamsHandler
(
    PlayerService playerService,
    ITeamService teamService,
    IValidatorService validatorService
) : IRequestHandler<AdvanceTeamsCommand>
{
    private readonly PlayerService _playerService = playerService;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validatorService;

    public async Task Handle(AdvanceTeamsCommand request, CancellationToken cancellationToken)
    {
        // sanitize input
        var finalTeamIds = request.TeamIds.Distinct().ToArray();

        await _validator
            .Auth(c => c.RequirePermissions(PermissionKey.Teams_Enroll))
            .AddEntityExistsValidator<Data.Game>(request.GameId)
            .AddValidator(async ctx =>
            {
                var captains = await _teamService.ResolveCaptains(request.TeamIds, cancellationToken);

                // ensure all teams are represented
                var unreppedTeamIds = finalTeamIds.Where(t => !captains.ContainsKey(t)).ToArray();
                if (unreppedTeamIds.Length > 0)
                {
                    foreach (var unreppedTeam in unreppedTeamIds)
                    {
                        ctx.AddValidationException(new ResourceNotFound<Team>(unreppedTeam));
                    }
                }
            })
            .Validate(cancellationToken);

        var gameId = await _teamService.GetGameId(request.TeamIds, cancellationToken);
        await _playerService.AdvanceTeams(new TeamAdvancement
        {
            GameId = gameId,
            NextGameId = request.GameId,
            TeamIds = request.TeamIds.ToArray(),
            WithScores = request.IncludeScores
        });
    }
}
