using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

internal class TeamChallengeScoreQueryHandler : IRequestHandler<TeamChallengeScoreQuery, TeamChallengeScoreSummary>
{
    private EntityExistsValidator<Data.Challenge> _challengeExists;
    private IScoringService _scoresService;
    private IValidatorService _validatorService;

    public TeamChallengeScoreQueryHandler(
        EntityExistsValidator<Data.Challenge> challengeExists,
        IScoringService scoresService,
        IValidatorService validatorService)
    {
        _challengeExists = challengeExists;
        _scoresService = scoresService;
        _validatorService = validatorService;
    }

    public async Task<TeamChallengeScoreSummary> Handle(TeamChallengeScoreQuery request, CancellationToken cancellationToken)
    {
        await _validatorService.Validate(request.challengeId, _challengeExists);

        return await _scoresService.GetTeamChallengeScore(request.challengeId);
    }
}
