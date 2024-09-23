using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Challenges;

public record GetChallengeSolutionGuideQuery(string ChallengeId) : IRequest<ChallengeSolutionGuide>;

internal class GetChallengeSolutionGuideHandler(
    EntityExistsValidator<GetChallengeSolutionGuideQuery, Data.Challenge> challengeExists,
    IStore store,
    IValidatorService<GetChallengeSolutionGuideQuery> validatorService
    ) : IRequestHandler<GetChallengeSolutionGuideQuery, ChallengeSolutionGuide>
{
    private readonly EntityExistsValidator<GetChallengeSolutionGuideQuery, Data.Challenge> _challengeExists = challengeExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<GetChallengeSolutionGuideQuery> _validatorService = validatorService;

    public async Task<ChallengeSolutionGuide> Handle(GetChallengeSolutionGuideQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config => config.RequireAuthentication())
            .AddValidator(_challengeExists.UseProperty(r => r.ChallengeId))
            .Validate(request, cancellationToken);

        var challenge = await _store.SingleAsync<Data.Challenge>(request.ChallengeId, cancellationToken);
        var spec = await _store.SingleAsync<Data.ChallengeSpec>(challenge.SpecId, cancellationToken);

        if (spec.SolutionGuideUrl.IsNotEmpty() && (challenge.PlayerMode == PlayerMode.Practice || spec.ShowSolutionGuideInCompetitiveMode))
        {
            return new ChallengeSolutionGuide
            {
                ChallengeSpecId = spec.Id,
                ShowInCompetitiveMode = spec.ShowSolutionGuideInCompetitiveMode,
                Url = spec.SolutionGuideUrl
            };
        }

        return null;
    }
}
