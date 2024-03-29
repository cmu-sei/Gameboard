using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record TeamChallengeScoreQuery(string ChallengeId) : IRequest<TeamChallengeScore>;

internal class TeamChallengeScoreQueryHandler : IRequestHandler<TeamChallengeScoreQuery, TeamChallengeScore>
{
    private readonly EntityExistsValidator<TeamChallengeScoreQuery, Data.Challenge> _challengeExists;
    private readonly IScoringService _scoresService;
    private readonly IValidatorService<TeamChallengeScoreQuery> _validatorService;

    public TeamChallengeScoreQueryHandler(
        EntityExistsValidator<TeamChallengeScoreQuery, Data.Challenge> challengeExists,
        IScoringService scoresService,
        IValidatorService<TeamChallengeScoreQuery> validatorService)
    {
        _challengeExists = challengeExists;
        _scoresService = scoresService;
        _validatorService = validatorService;
    }

    public async Task<TeamChallengeScore> Handle(TeamChallengeScoreQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));
        await _validatorService.Validate(request, cancellationToken);

        return await _scoresService.GetTeamChallengeScore(request.ChallengeId);
    }
}
