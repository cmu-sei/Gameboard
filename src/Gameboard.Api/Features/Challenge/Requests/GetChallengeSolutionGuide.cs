using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Challenges;

public record GetChallengeSolutionGuideQuery(string ChallengeId) : IRequest<ChallengeSolutionGuide>;

internal class GetChallengeSolutionGuideHandler : IRequestHandler<GetChallengeSolutionGuideQuery, ChallengeSolutionGuide>
{
    private readonly EntityExistsValidator<GetChallengeSolutionGuideQuery, Data.Challenge> _challengeExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetChallengeSolutionGuideQuery> _validatorService;

    public GetChallengeSolutionGuideHandler
    (
        EntityExistsValidator<GetChallengeSolutionGuideQuery, Data.Challenge> challengeExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetChallengeSolutionGuideQuery> validatorService
    )
    {
        _challengeExists = challengeExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<ChallengeSolutionGuide> Handle(GetChallengeSolutionGuideQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Member)
            .Authorize();

        _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));
        await _validatorService.Validate(request, cancellationToken);

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
