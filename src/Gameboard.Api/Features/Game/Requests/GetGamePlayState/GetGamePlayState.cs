using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.Start;

public record GetGamePlayStateQuery(string TeamId, string ActingUserId) : IRequest<GamePlayState>;

internal class GetGamePlayStateHandler : IRequestHandler<GetGamePlayStateQuery, GamePlayState>
{
    private readonly EntityExistsValidator<GetGamePlayStateQuery, Data.Game> _gameExists;
    private readonly IGameModeServiceFactory _gameModeServiceFactory;
    private readonly IGameService _gameService;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly EntityExistsValidator<GetGamePlayStateQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetGamePlayStateQuery> _validatorService;

    public GetGamePlayStateHandler
    (
        EntityExistsValidator<GetGamePlayStateQuery, Data.Game> gameExists,
        IGameModeServiceFactory gameModeServiceFactory,
        IGameService gameService,
        INowService now,
        IStore store,
        ITeamService teamService,
        EntityExistsValidator<GetGamePlayStateQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetGamePlayStateQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameModeServiceFactory = gameModeServiceFactory;
        _gameService = gameService;
        _now = now;
        _store = store;
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

        // default rules that apply to all sessions
        var teamSession = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .Select(p => new
            {
                p.SessionBegin,
                p.SessionEnd,
                p.Role
            })
            .ToArrayAsync(cancellationToken);

        var begin = teamSession.Select(p => p.SessionBegin).Distinct().Single();
        var end = teamSession.Select(p => p.SessionEnd).Distinct().Single();

        if (begin.IsEmpty())
            return GamePlayState.NotStarted;

        var nowish = _now.Get();
        if (begin <= nowish && (end.IsEmpty() || end >= nowish))
            return GamePlayState.Started;

        if (nowish > end)
            return GamePlayState.GameOver;

        var modeService = await _gameModeServiceFactory.Get(gameId);
        return await modeService.GetGamePlayStateForTeam(request.TeamId, cancellationToken);
    }
}
