using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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

public record GetUserActiveChallengesQuery(string UserId) : IRequest<IEnumerable<ActiveChallenge>>;

internal class GetUserActiveChallengesHandler : IRequestHandler<GetUserActiveChallengesQuery, IEnumerable<ActiveChallenge>>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITimeWindowService _timeWindowService;
    private readonly EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetUserActiveChallengesQuery> _validator;

    public GetUserActiveChallengesHandler
    (
        JsonSerializerOptions jsonSerializerOptions,
        IMapper mapper,
        INowService now,
        IStore store,
        ITimeWindowService timeWindowService,
        EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetUserActiveChallengesQuery> validator
    )
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _mapper = mapper;
        _now = now;
        _store = store;
        _timeWindowService = timeWindowService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<IEnumerable<ActiveChallenge>> Handle(GetUserActiveChallengesQuery request, CancellationToken cancellationToken)
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
            .OrderByDescending(c => c.Player.SessionEnd)
            .Select(c => new ActiveChallenge
            {
                // have to join spec separately later to get the names/tags
                ChallengeSpec = new ActiveChallengeSpec
                {
                    Id = c.SpecId,
                    Name = null,
                    Tag = null
                },
                Game = new SimpleEntity { Id = c.GameId, Name = c.Game.Name },
                Player = new SimpleEntity { Id = c.PlayerId, Name = c.Player.ApprovedName },
                User = new SimpleEntity { Id = c.Player.UserId, Name = c.Player.User.ApprovedName },
                ChallengeDeployment = new ActiveChallengeDeployment
                {
                    ChallengeId = c.Id,
                    Vms = c.BuildGameEngineState(_mapper, _jsonSerializerOptions).Vms
                },
                TeamId = c.Player.TeamId,
                Session = _timeWindowService.CreateWindow(_now.Get(), c.Player.SessionBegin, c.Player.SessionEnd),
                Start = c.Player.SessionBegin,
                End = c.Player.SessionEnd,
                HasDeployedGamespace = c.HasDeployedGamespace,
                PlayerMode = c.PlayerMode,
                MaxPossibleScore = c.Points,
                Score = new decimal(c.Score),
            })
            .ToListAsync(cancellationToken);

        // load the spec names
        var specIds = challenges.Select(c => c.ChallengeSpec.Id).ToList();
        var specs = await _store
            .List<Data.ChallengeSpec>()
            .Where(s => specIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        foreach (var challenge in challenges)
        {
            if (specs.ContainsKey(challenge.ChallengeSpec.Id))
            {
                challenge.ChallengeSpec.Name = specs[challenge.ChallengeSpec.Id].Name;
                challenge.ChallengeSpec.Tag = specs[challenge.ChallengeSpec.Id].Tag;
            }
        }

        return challenges;
    }
}
