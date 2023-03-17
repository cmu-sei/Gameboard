using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

internal class TeamGameScoreQueryHandler : IRequestHandler<TeamGameScoreQuery, TeamGameScoreSummary>
{
    private readonly TeamExistsValidator _teamExists;
    private readonly IScoringService _scoreService;
    private readonly IValidatorService _validatorService;

    public TeamGameScoreQueryHandler(
        IScoringService scoreService,
        TeamExistsValidator teamExists,
        IValidatorService validatorService)
    {
        _scoreService = scoreService;
        _teamExists = teamExists;
        _validatorService = validatorService;
    }

    public async Task<TeamGameScoreSummary> Handle(TeamGameScoreQuery request, CancellationToken cancellationToken)
    {
        await _validatorService.Validate(request.teamId, _teamExists);

        return await _scoreService
            .GetTeamGameScore(request.teamId);
    }
}
