using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Challenges;

public record GetChallengeProgressQuery(string ChallengeId) : IRequest<GetChallengeProgressResponse>;

internal class GetChallengeProgressHandler
(
    IActingUserService actingUserService,
    EntityExistsValidator<Data.Challenge> challengeExists,
    ChallengeService challengeService,
    IGameEngineService gameEngine,
    IValidatorService validatorService
) : IRequestHandler<GetChallengeProgressQuery, GetChallengeProgressResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists = challengeExists;
    private readonly ChallengeService _challengeService = challengeService;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetChallengeProgressResponse> Handle(GetChallengeProgressQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth
            (
                c => c
                    .RequirePermissions(Users.PermissionKey.Teams_Observe)
                    .Unless(() => _challengeService.UserIsPlayingChallenge(request.ChallengeId, _actingUserService.Get()?.Id))
            )
            .AddValidator(_challengeExists.UseValue(request.ChallengeId))
            .Validate(cancellationToken);

        return new GetChallengeProgressResponse
        {
            Progress = await _gameEngine.GetChallengeProgress(request.ChallengeId, GameEngineType.TopoMojo, cancellationToken)
        };
    }
}
