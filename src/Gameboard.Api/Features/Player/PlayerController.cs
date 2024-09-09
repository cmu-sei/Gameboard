
// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class PlayerController
    (
        IActingUserService actingUserService,
        ILogger<PlayerController> logger,
        IDistributedCache cache,
        PlayerValidator validator,
        IMediator mediator,
        PlayerService playerService,
        IMapper _mapper,
        IUserRolePermissionsService permissionsService,
        ITeamService teamService
        ) : _Controller(actingUserService, logger, cache, validator)
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;

    private readonly PlayerService PlayerService = playerService;
    private readonly IMapper Mapper = _mapper;
    private readonly ITeamService _teamService = teamService;

    /// <summary>
    /// Enrolls a user in a game.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A player record which represents an instance of the user playing a given game.</returns>
    [HttpPost("api/player")]
    [Authorize]
    public async Task<Player> Enroll([FromBody] NewPlayer model, CancellationToken cancellationToken)
    {
        await AuthorizeAny
        (
            _permissionsService.Can(PermissionKey.Teams_Enroll),
            Task.FromResult(model.UserId == Actor.Id)
        );

        await Validate(model);
        return await PlayerService.Enroll(model, Actor, cancellationToken);
    }

    /// <summary>
    /// Retrieve player
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/player/{id}")]
    [Authorize]
    public async Task<Player> Retrieve([FromRoute] string id)
    {
        // TODO: consider appropriate authorization
        // Note: this is essentially a scoreboard entry
        AuthorizeAll();

        await Validate(new Entity { Id = id });

        return await PlayerService.Retrieve(id);
    }

    /// <summary>
    /// Change player
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/player")]
    [Authorize]
    public async Task<PlayerUpdatedViewModel> Update([FromBody] ChangedPlayer model)
    {
        await AuthorizeAny
        (
            _permissionsService.IsActingUserAsync(model.Id),
            _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges)
        );

        await Validate(model);

        var result = await PlayerService.Update(model, Actor, await _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges));
        return Mapper.Map<PlayerUpdatedViewModel>(result);
    }

    [HttpPut("api/player/{playerId}/ready")]
    [Authorize]
    public Task UpdatePlayerReady([FromRoute] string playerId, [FromBody] PlayerReadyUpdate readyUpdate)
        => _mediator.Send(new UpdatePlayerReadyStateCommand(playerId, readyUpdate.IsReady, Actor));

    [HttpPut("api/player/{playerId}/start")]
    [Authorize]
    public async Task<Player> Start(string playerId)
    {
        await AuthorizeAny
        (
            IsSelf(playerId),
            _permissionsService.Can(PermissionKey.Teams_EditSession)
        );

        var sessionStartRequest = new SessionStartRequest { PlayerId = playerId };
        await Validate(sessionStartRequest);
        return await PlayerService.StartSession(sessionStartRequest, Actor, await _permissionsService.Can(PermissionKey.Teams_EditSession));
    }

    /// <summary>
    /// Delete a player enrollment
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="asAdmin"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("/api/player/{playerId}")]
    [Authorize]
    public async Task Unenroll([FromRoute] string playerId, [FromQuery] bool asAdmin, CancellationToken cancellationToken)
    {
        await AuthorizeAny
        (
            IsSelf(playerId),
            _permissionsService.Can(PermissionKey.Teams_Enroll)
        );

        var unenrollRequest = new PlayerUnenrollRequest
        {
            Actor = Actor,
            PlayerId = playerId
        };

        await Validate(unenrollRequest);
        await PlayerService.Unenroll(unenrollRequest, cancellationToken);
    }

    /// <summary>
    /// Find players
    /// </summary>
    /// <remarks>
    /// Filter with query params `gid, tid, uid, org` (group, team, user, sponsor ids)
    /// Filter with query param `filter=collapse` to pull just one player record per team.
    /// </remarks>
    /// <param name="model">PlayerDataFilter</param>
    /// <returns></returns>
    [HttpGet("/api/players")]
    [AllowAnonymous]
    public async Task<Player[]> List([FromQuery] PlayerDataFilter model)
    {
        return await PlayerService.List(model, await _permissionsService.Can(PermissionKey.Admin_View));
    }

    /// <summary>
    /// Load active challenge data for a team.
    /// </summary>
    /// <param name="id">The id of the team who owns the challenges</param>
    /// <returns>An array of challenge entries.</returns>
    [HttpGet("/api/team/{id}/challenges")]
    [Authorize]
    public async Task<IEnumerable<TeamChallenge>> GetTeamChallenges([FromRoute] string id)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
        return await PlayerService.LoadChallengesForTeam(id);
    }

    /// <summary>
    /// Get a Game's Teams with Members
    /// </summary>
    /// <param name="id">Game Id</param>
    /// <returns>Team[]</returns>
    [HttpGet("/api/teams/observe/{id}")]
    [Authorize]
    public async Task<IEnumerable<ObserveTeam>> ObserveTeams([FromRoute] string id)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
        return await PlayerService.ObserveTeams(id);
    }

    /// <summary>
    /// Get Player Team
    /// </summary>
    /// <param name="id">player id</param>
    /// <returns>Team</returns>
    [HttpGet("/api/board/{id}")]
    [Authorize]
    public async Task<BoardPlayer> GetBoard([FromRoute] string id)
    {
        await Validate(new Entity { Id = id });
        await AuthorizeAny(IsSelf(id));

        return await PlayerService.LoadBoard(id);
    }

    [HttpPost("/api/player/{id}/invite")]
    [Authorize]
    public async Task<TeamInvitation> Invite([FromRoute] string id)
    {
        await AuthorizeAny
        (
            _permissionsService.IsActingUserAsync(id),
            _permissionsService.Can(PermissionKey.Teams_Enroll)
        );

        await Validate(new Entity { Id = id });

        return await PlayerService.GenerateInvitation(id);
    }

    /// <summary>
    /// Enlists the user into a player team
    /// </summary>
    /// <param name="model">EnlistingPlayer</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("/api/player/enlist")]
    [Authorize]
    public Task<Player> Enlist([FromBody] PlayerEnlistment model, CancellationToken cancellationToken)
        => _mediator.Send(new AddPlayerToTeamCommand(model.PlayerId, model.Code), cancellationToken);

    [HttpPut("/api/team/{teamId}/manager/{playerId}")]
    [Authorize]
    public async Task PromoteToCaptain(string teamId, string playerId, [FromBody] PromoteToManagerRequest promoteRequest, CancellationToken cancellationToken)
    {
        await AuthorizeAny
        (
            _permissionsService.Can(PermissionKey.Teams_Enroll),
            IsSelf(promoteRequest.CurrentManagerPlayerId)
        );

        var model = new PromoteToManagerRequest
        {
            Actor = Actor,
            CurrentManagerPlayerId = promoteRequest.CurrentManagerPlayerId,
            NewManagerPlayerId = playerId,
            TeamId = teamId
        };

        await Validate(model);
        await _teamService.PromoteCaptain(teamId, playerId, Actor, cancellationToken);
    }

    /// <summary>
    /// Get Player Certificate
    /// </summary>
    /// <param name="id">player id</param>
    /// <returns></returns>
    [HttpGet("/api/certificate/{id}")]
    [Authorize]
    public async Task<PlayerCertificate> GetCertificate([FromRoute] string id)
    {
        await Validate(new Entity { Id = id });
        await AuthorizeAny(IsSelf(id));

        return await PlayerService.MakeCertificate(id);
    }

    /// <summary>
    /// Get List of Player Certificates
    /// </summary>
    /// <returns> </returns>
    [HttpGet("/api/certificates")]
    [Authorize]
    public async Task<IEnumerable<PlayerCertificate>> GetCertificates()
    {
        return await PlayerService.MakeCertificates(Actor.Id);
    }

    private async Task<bool> IsSelf(string playerId)
    {
        return await PlayerService.MapId(playerId) == Actor.Id;
    }
}
