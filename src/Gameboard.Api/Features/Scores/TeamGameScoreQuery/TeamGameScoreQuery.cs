using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record TeamGameScoreQuery(string TeamId) : IRequest<TeamGameScore>;

internal class TeamGameScoreQueryHandler : IRequestHandler<TeamGameScoreQuery, TeamGameScore>
{
    private readonly TeamExistsValidator<TeamGameScoreQuery> _teamExists;
    private readonly IScoringService _scoreService;
    private readonly IValidatorService<TeamGameScoreQuery> _validatorService;

    public TeamGameScoreQueryHandler(
        IScoringService scoreService,
        TeamExistsValidator<TeamGameScoreQuery> teamExists,
        IValidatorService<TeamGameScoreQuery> validatorService)
    {
        _scoreService = scoreService;
        _teamExists = teamExists;
        _validatorService = validatorService;
    }

    public async Task<TeamGameScore> Handle(TeamGameScoreQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        await _validatorService.Validate(request, cancellationToken);

        return await _scoreService
            .GetTeamGameScore(request.TeamId);
    }
}
