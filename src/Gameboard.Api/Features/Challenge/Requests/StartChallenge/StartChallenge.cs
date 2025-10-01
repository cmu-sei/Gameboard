// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public record StartChallengeCommand(string ChallengeSpecId, string TeamId, int variantIndex) : IRequest<StartChallengeResponse>;

internal sealed class StartChallengeHandler
(
    IActingUserService actingUser,
    ChallengeService challengeService,
    IHubContext<AppHub, IAppHubEvent> legacyTeamHub,
    IStore store,
    ITeamService teamService,
    IValidatorService validator
) : IRequestHandler<StartChallengeCommand, StartChallengeResponse>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly ChallengeService _challengeService = challengeService;
    private readonly IHubContext<AppHub, IAppHubEvent> _legacyTeamHub = legacyTeamHub;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validator;

    public async Task<StartChallengeResponse> Handle(StartChallengeCommand request, CancellationToken cancellationToken)
    {
        // resolve the team/challenge/game mess
        var captain = await _teamService.ResolveCaptain(request.TeamId, cancellationToken);
        var team = await _teamService.GetTeam(request.TeamId);

        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Id == request.ChallengeSpecId)
            .Select(s => new { s.Id, s.GameId, GameName = s.Game.Name })
            .SingleOrDefaultAsync(cancellationToken);

        await _validator
            .Auth
            (
                c => c
                    .RequireOneOf(PermissionKey.Teams_EditSession)
                    .UnlessUserIdIn([.. team.Members.Select(m => m.Id)])
            )
            .AddValidator(spec is null, new ResourceNotFound<Data.ChallengeSpec>(request.ChallengeSpecId))
            .AddValidator(captain.GameId != spec.GameId, new TeamIsntPlayingGame
            (
                new SimpleEntity
                {
                    Id = captain.TeamId,
                    Name = captain.ApprovedName
                },
                new SimpleEntity
                {
                    Id = captain.GameId,
                    Name = spec.GameName
                }
            ))
            .Validate(cancellationToken);

        var challenge = await _challengeService.GetOrCreate(new NewChallenge
        {
            PlayerId = captain.Id,
            SpecId = request.ChallengeSpecId,
            StartGamespace = true,
            Variant = request.variantIndex
        });

        await _legacyTeamHub.Clients.Group(captain.TeamId).ChallengeEvent(new HubEvent<Challenge>
        {
            Model = challenge,
            Action = EventAction.Updated,
            ActingUser = _actingUser.Get().ToSimpleEntity()
        });

        return new StartChallengeResponse { Challenge = challenge };
    }
}
