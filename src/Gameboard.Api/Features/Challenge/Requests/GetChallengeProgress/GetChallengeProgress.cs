using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public record GetChallengeProgressQuery(string ChallengeId) : IRequest<GetChallengeProgressResponse>;

internal class GetChallengeProgressHandler
(
    IActingUserService actingUserService,
    EntityExistsValidator<Data.Challenge> challengeExists,
    ChallengeService challengeService,
    IGameEngineService gameEngine,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetChallengeProgressQuery, GetChallengeProgressResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists = challengeExists;
    private readonly ChallengeService _challengeService = challengeService;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetChallengeProgressResponse> Handle(GetChallengeProgressQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth
            (
                c => c
                    .Require(Users.PermissionKey.Teams_Observe)
                    .Unless(() => _challengeService.UserIsPlayingChallenge(request.ChallengeId, _actingUserService.Get()?.Id))
            )
            .AddValidator(_challengeExists.UseValue(request.ChallengeId))
            .Validate(cancellationToken);

        var progress = await _gameEngine.GetChallengeProgress(request.ChallengeId, GameEngineType.TopoMojo, cancellationToken);
        var challengeData = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == progress.Id)
            // more team hacks
            .OrderByDescending(c => c.Player.Role)
            .Select(c => new
            {
                Spec = new SimpleEntity { Id = c.SpecId, Name = c.Name },
                Team = new SimpleEntity { Id = c.TeamId, Name = c.Player.ApprovedName }
            })
            .FirstAsync(cancellationToken);

        return new GetChallengeProgressResponse
        {
            Progress = progress,
            Spec = challengeData.Spec,
            Team = challengeData.Team
        };
    }
}
