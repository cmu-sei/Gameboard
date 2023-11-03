using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games.Start;

public record GetGameStartPhaseQuery(string GameId, string TeamId, string ActingUserId) : IRequest<GameStartPhase>;

internal class GetGameStartPhaseHandler : IRequestHandler<GetGameStartPhaseQuery, GameStartPhase>
{
    private readonly EntityExistsValidator<GetGameStartPhaseQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IGameStartService _gameStartService;
    private readonly EntityExistsValidator<GetGameStartPhaseQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetGameStartPhaseQuery> _validatorService;

    public GetGameStartPhaseHandler
    (
        EntityExistsValidator<GetGameStartPhaseQuery, Data.Game> gameExists,
        IGameService gameService,
        IGameStartService gameStartService,
        EntityExistsValidator<GetGameStartPhaseQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetGameStartPhaseQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _gameStartService = gameStartService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<GameStartPhase> Handle(GetGameStartPhaseQuery request, CancellationToken cancellationToken)
    {
        // authorize
        var isPlaying = await _gameService.IsUserPlaying(request.GameId, request.ActingUserId);
        if (!isPlaying)
            _userRoleAuthorizer
                .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support)
                .Authorize();

        // validate
        _validatorService
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator(_userExists.UseProperty(r => r.ActingUserId));
        await _validatorService.Validate(request, cancellationToken);

        return await _gameStartService.GetGameStartPhase(request.GameId, request.TeamId, cancellationToken);
    }
}
