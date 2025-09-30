// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.Start;

public record GetGamePlayStateQuery(string TeamId, string ActingUserId) : IRequest<GamePlayState>;

internal class GetGamePlayStateHandler(
    IGameModeServiceFactory gameModeServiceFactory,
    IGameService gameService,
    INowService now,
    IStore store,
    ITeamService teamService,
    EntityExistsValidator<GetGamePlayStateQuery, Data.User> userExists,
    IValidatorService<GetGamePlayStateQuery> validatorService
    ) : IRequestHandler<GetGamePlayStateQuery, GamePlayState>
{
    private readonly IGameModeServiceFactory _gameModeServiceFactory = gameModeServiceFactory;
    private readonly IGameService _gameService = gameService;
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly EntityExistsValidator<GetGamePlayStateQuery, Data.User> _userExists = userExists;
    private readonly IValidatorService<GetGamePlayStateQuery> _validatorService = validatorService;

    public async Task<GamePlayState> Handle(GetGamePlayStateQuery request, CancellationToken cancellationToken)
    {
        // authorize
        var gameId = await _teamService.GetGameId(request.TeamId, cancellationToken);

        await _validatorService
            .Auth
            (
                config => config
                    .Require(Users.PermissionKey.Admin_View)
                    .Unless(() => _gameService.IsUserPlaying(gameId, request.ActingUserId))
            )
            .AddValidator(_userExists.UseProperty(r => r.ActingUserId))
            .AddValidator((req, ctx) =>
            {
                if (gameId.IsEmpty())
                {
                    throw new ResourceNotFound<Team>(request.TeamId);
                }
            })
            .Validate(request, cancellationToken);

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

        // max is a kludge here - there should only be one, but 
        // becaues of denormalized structure, bugs could cause more than one
        var begin = teamSession.Select(p => p.SessionBegin).Max();
        var end = teamSession.Select(p => p.SessionEnd).Max();

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
