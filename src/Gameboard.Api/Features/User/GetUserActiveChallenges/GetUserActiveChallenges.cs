using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record GetUserActiveChallengesQuery(string UserId) : IRequest<UserActiveChallenges>;

public sealed class UserActiveChallenges
{
    public required SimpleEntity User { get; set; }
    public required IEnumerable<ActiveChallenge> Practice { get; set; }
    public required IEnumerable<ActiveChallenge> Competition { get; set; }
}

internal class GetUserActiveChallengesHandler : IRequestHandler<GetUserActiveChallengesQuery, UserActiveChallenges>
{
    private readonly IGameEngineService _gameEngine;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITimeWindowService _timeWindowService;
    private readonly EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetUserActiveChallengesQuery> _validator;

    public GetUserActiveChallengesHandler
    (
        IGameEngineService gameEngine,
        INowService now,
        IStore store,
        ITimeWindowService timeWindowService,
        EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetUserActiveChallengesQuery> validator
    )
    {
        _gameEngine = gameEngine;
        _now = now;
        _store = store;
        _timeWindowService = timeWindowService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<UserActiveChallenges> Handle(GetUserActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_userExists.UseProperty(m => m.UserId));
        await _validator.Validate(request);

        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin };
        _userRoleAuthorizer.AllowedUserId = request.UserId;
        _userRoleAuthorizer.Authorize();

        // retrieve stuff (initial pull from DB side eval)
        var user = await _store
            .List<Data.User>()
            .Select(u => new SimpleEntity { Id = u.Id, Name = u.ApprovedName })
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var challenges = await _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Player.SessionBegin >= DateTimeOffset.MinValue)
            .Where(c => c.Player.SessionEnd > _now.Get())
            .Where(c => c.Player.UserId == request.UserId)
            .OrderByDescending(c => c.Player.SessionEnd)
            .Select(c => new
            {
                // have to join spec separately later to get the names/other properties
                Spec = new ActiveChallengeSpec
                {
                    Id = c.SpecId,
                    Name = null,
                    Tag = null,
                    AverageDeploySeconds = 0,
                },
                Game = new SimpleEntity { Id = c.GameId, Name = c.Game.Name },
                c.GameEngineType,
                Player = new SimpleEntity { Id = c.PlayerId, Name = c.Player.ApprovedName },
                User = new SimpleEntity { Id = c.Player.UserId, Name = c.Player.User.ApprovedName },
                ChallengeDeployment = new ActiveChallengeDeployment
                {
                    ChallengeId = c.Id,
                    // these are dummy values we'll fill out below (can't do it here because we're on the db server side during the query)
                    Markdown = string.Empty,
                    IsDeployed = false,
                    Vms = Array.Empty<GameEngineVmState>()
                },
                c.Player.TeamId,
                Session = _timeWindowService.CreateWindow(_now.Get(), c.Player.SessionBegin, c.Player.SessionEnd),
                c.PlayerMode,
                // additional dummy values - we'll get the real attempts and stuff from state
                ScoreAndAttemptsState = new ActiveChallengeScoreAndAttemptsState
                {
                    Score = new decimal(c.Score),
                    MaxPossibleScore = c.Points,
                    Attempts = 0,
                    MaxAttempts = 0
                },
                c.State
            })
            .ToListAsync(cancellationToken);

        // load the spec names and set state properties
        var specIds = challenges.Select(c => c.Spec.Id).ToList();
        var specs = await _store
            .List<Data.ChallengeSpec>()
            .Where(s => specIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        // now do client side eval
        foreach (var challenge in challenges)
        {
            if (specs.ContainsKey(challenge.Spec.Id))
            {
                challenge.Spec.Name = specs[challenge.Spec.Id].Name;
                challenge.Spec.Tag = specs[challenge.Spec.Id].Tag;
                challenge.Spec.AverageDeploySeconds = specs[challenge.Spec.Id].AverageDeploySeconds;
            }

            // we need the state json as an object so we don't lose our minds and to make some decisions
            var state = await _gameEngine.GetChallengeState(challenge.GameEngineType, challenge.State);

            // set attempt info
            challenge.ScoreAndAttemptsState.Attempts = state.Challenge.Attempts;
            challenge.ScoreAndAttemptsState.MaxAttempts = state.Challenge.MaxAttempts;

            // currently, topomojo sends an empty VM list when the vms are turned off, so we use this to 
            // proxy whether the challenge is deployed. hopefully topo will eventually send VMs with
            // isRunning = false when asked, so we're making these separate concepts on the API surface
            challenge.ChallengeDeployment.IsDeployed = state.Vms.Any();
            challenge.ChallengeDeployment.Vms = state.Vms;
            // now that we have the state, we can also read the final challenge document (which may different than the spec due to transforms or etc.)
            challenge.ChallengeDeployment.Markdown = state.Markdown;
        }

        var typedChallenges = challenges.Select(c => new ActiveChallenge
        {
            Spec = c.Spec,
            Game = c.Game,
            Player = c.Player,
            User = c.User,
            ChallengeDeployment = c.ChallengeDeployment,
            TeamId = c.TeamId,
            PlayerMode = c.PlayerMode,
            ScoreAndAttemptsState = c.ScoreAndAttemptsState,
            Session = c.Session
        })
        // now that we have info about points and attempts, we can reason about whether we should return
        // challenges that are maxed out on score or guesses
        .Where(c => c.ScoreAndAttemptsState.Score < c.ScoreAndAttemptsState.MaxPossibleScore)
        .Where(c => c.ScoreAndAttemptsState.Attempts < c.ScoreAndAttemptsState.MaxAttempts);

        return new UserActiveChallenges
        {
            User = user,
            Competition = typedChallenges.Where(c => c.PlayerMode == PlayerMode.Competition),
            Practice = typedChallenges.Where(c => c.PlayerMode == PlayerMode.Practice)
        };
    }
}
