// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeSessionQuery(string UserId) : IRequest<PracticeSession>;

internal sealed class GetPracticeSessionHandler
(
    IActingUserService actingUserService,
    INowService nowService,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
) : IRequestHandler<GetPracticeSessionQuery, PracticeSession>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly INowService _nowService = nowService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<PracticeSession> Handle(GetPracticeSessionQuery request, CancellationToken cancellationToken)
    {
        var actingUser = _actingUserService.Get();

        await _validatorService
            .Auth
            (
                config => config
                    .Require(PermissionKey.Teams_Observe)
                    .UnlessUserIdIn(request.UserId)
            )
            .Validate(cancellationToken);

        var teamIds = await _teamService.GetUserTeamIds(request.UserId);

        return await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Mode == PlayerMode.Practice)
            .Where(p => p.SessionEnd > _nowService.Get() || p.SessionEnd == DateTimeOffset.MinValue)
            .Where(p => p.UserId == request.UserId)
            .Select(p => new PracticeSession
            {
                GameId = p.GameId,
                PlayerId = p.Id,
                TeamId = p.TeamId,
                UserId = p.UserId,
                Session = new TimestampRange
                {
                    Start = p.SessionBegin.ToEpochMs(),
                    End = p.SessionEnd.ToEpochMs()
                }
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
