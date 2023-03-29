using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

internal class TeamGameScoreQueryHandler : IRequestHandler<TeamGameScoreQuery, TeamGameScoreSummary>
{
    private readonly TeamExistsValidator<TeamGameScoreQuery> _teamExists;
    private readonly IScoringService _scoreService;
    private readonly TeamGameScoreQueryValidator _validator;

    public TeamGameScoreQueryHandler(
        IScoringService scoreService,
        TeamExistsValidator<TeamGameScoreQuery> teamExists,
        TeamGameScoreQueryValidator validator)
    {
        _scoreService = scoreService;
        _teamExists = teamExists;
        _validator = validator;
    }

    public async Task<TeamGameScoreSummary> Handle(TeamGameScoreQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request);

        return await _scoreService
            .GetTeamGameScore(request.teamId);
    }
}
