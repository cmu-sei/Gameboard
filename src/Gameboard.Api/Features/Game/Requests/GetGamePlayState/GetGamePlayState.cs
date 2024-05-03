using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games.Start;

public record GetGamePlayStateQuery(string TeamId, string ActingUserId) : IRequest<GamePlayState>;

internal class GetGamePlayStateHandler : IRequestHandler<GetGamePlayStateQuery, GamePlayState>
{
    private readonly EntityExistsValidator<GetGamePlayStateQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IGameStartService _gameStartService;
    private readonly ITeamService _teamService;
    private readonly EntityExistsValidator<GetGamePlayStateQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetGamePlayStateQuery> _validatorService;

    public GetGamePlayStateHandler
    (
        EntityExistsValidator<GetGamePlayStateQuery, Data.Game> gameExists,
        IGameService gameService,
        IGameStartService gameStartService,
        ITeamService teamService,
        EntityExistsValidator<GetGamePlayStateQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetGamePlayStateQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _gameStartService = gameStartService;
        _teamService = teamService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<GamePlayState> Handle(GetGamePlayStateQuery request, CancellationToken cancellationToken)
    {
        // authorize
        var gameId = await _teamService.GetGameId(request.TeamId, cancellationToken);

        var isPlaying = await _gameService.IsUserPlaying(gameId, request.ActingUserId);
        if (!isPlaying)
            _userRoleAuthorizer
                .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support)
                .Authorize();

        // validate
        _validatorService.AddValidator((req, ctx) =>
        {
            if (gameId.IsEmpty())
                ctx.AddValidationException(new TeamHasNoPlayersException(request.TeamId));
        })
        .AddValidator(_userExists.UseProperty(r => r.ActingUserId));
        await _validatorService.Validate(request, cancellationToken);

        return await _gameStartService.GetGamePlayState(request.TeamId, cancellationToken);
    }
}
