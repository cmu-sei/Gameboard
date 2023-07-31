using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record GetUserActiveChallengesQuery(string UserId) : IRequest<IEnumerable<UserChallengeSlim>>;

internal class GetUserActiveChallengesHandler : IRequestHandler<GetUserActiveChallengesQuery, IEnumerable<UserChallengeSlim>>
{
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITimeWindowService _timeWindowService;
    private readonly EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetUserActiveChallengesQuery> _validator;

    public GetUserActiveChallengesHandler
    (
        INowService now,
        IStore store,
        ITimeWindowService timeWindowService,
        EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetUserActiveChallengesQuery> validator
    )
    {
        _now = now;
        _store = store;
        _timeWindowService = timeWindowService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<IEnumerable<UserChallengeSlim>> Handle(GetUserActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_userExists.UseProperty(m => m.UserId));
        await _validator.Validate(request);

        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin };
        _userRoleAuthorizer.AllowedUserId = request.UserId;
        _userRoleAuthorizer.Authorize();

        // retrieve stuff
        var challenges = await _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Player.SessionBegin >= DateTimeOffset.MinValue)
            .Where(c => c.Player.SessionEnd > _now.Get())
            .Where(c => c.PlayerMode == PlayerMode.Practice)
            .Where(c => c.Player.UserId == request.UserId)
            .OrderByDescending(c => c.Player.SessionBegin)
            .Select(c => new UserChallengeSlim
            {
                Challenge = new SimpleEntity { Id = c.Id, Name = c.Name },
                Game = new SimpleEntity { Id = c.GameId, Name = c.Game.Name },
                Player = new SimpleEntity { Id = c.PlayerId, Name = c.Player.ApprovedName },
                User = new SimpleEntity { Id = c.Player.UserId, Name = c.Player.User.ApprovedName },
                SpecId = c.SpecId,
                TeamId = c.Player.TeamId,
                Session = _timeWindowService.CreateWindow(c.Player.SessionBegin, c.Player.SessionEnd),
                HasDeployedGamespace = c.HasDeployedGamespace,
                PlayerMode = c.PlayerMode,
                MaxPossibleScore = c.Points,
                Score = new decimal(c.Score),
            })

            .ToListAsync(cancellationToken);

        return challenges;
    }
}
