using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public sealed class AdminEnrollTeamResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string GameId { get; set; }
    public required IEnumerable<PlayerWithSponsor> Players { get; set; }
}

public record AdminEnrollTeamRequest
(
    string GameId,
    IEnumerable<string> UserIds,
    string CaptainUserId = null,
    PlayerMode PlayerMode = PlayerMode.Competition
) : IRequest<AdminEnrollTeamResponse>;

internal class AdminEnrollTeamHandler : IRequestHandler<AdminEnrollTeamRequest, AdminEnrollTeamResponse>
{
    private readonly IActingUserService _actingUserService;
    private readonly IGuidService _guids;
    private readonly PlayerService _playerService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<AdminEnrollTeamRequest> _validator;

    public AdminEnrollTeamHandler
    (
        IActingUserService actingUserService,
        IGuidService guids,
        PlayerService playerService,
        IStore store,
        ITeamService teamService,
        IGameboardRequestValidator<AdminEnrollTeamRequest> validator
    )
    {
        _actingUserService = actingUserService;
        _guids = guids;
        _playerService = playerService;
        _store = store;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task<AdminEnrollTeamResponse> Handle(AdminEnrollTeamRequest request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _validator.Validate(request, cancellationToken);

        // get the acting admin
        var actingUser = _actingUserService.Get();

        // enlist all and retain the ids
        string teamUpCode = null;
        var createdPlayers = new List<Api.Player>();
        Api.Player captainPlayer = null;

        foreach (var userId in request.UserIds)
        {
            var newPlayer = await _playerService.Enroll(new NewPlayer { GameId = request.GameId, UserId = userId }, actingUser, cancellationToken);
            createdPlayers.Add(newPlayer);

            // if this is the captain (or if one wasn't specified), remember them for team-up purposes
            if ((request.CaptainUserId.IsNotEmpty() && userId == request.CaptainUserId) || request.CaptainUserId.IsEmpty())
            {
                captainPlayer = newPlayer;
                teamUpCode = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.Id == newPlayer.Id)
                    .Select(p => p.InviteCode)
                    .SingleAsync(cancellationToken);
            }
        }

        // by default, players don't have an invitation code, so make one for the captain.
        var invitationCode = _guids.GetGuid();
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == captainPlayer.Id)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.InviteCode, invitationCode));

        // team everyone up
        foreach (var player in createdPlayers.Where(p => p.Id != captainPlayer.Id))
            await _playerService.Enlist(new PlayerEnlistment { Code = teamUpCode, PlayerId = player.Id }, actingUser, cancellationToken);

        // make the captain the actual captain
        await _teamService.PromoteCaptain(captainPlayer.TeamId, captainPlayer.Id, actingUser, cancellationToken);

        return new AdminEnrollTeamResponse
        {
            Id = captainPlayer.TeamId,
            Name = captainPlayer.ApprovedName,
            GameId = captainPlayer.GameId,
            Players = createdPlayers.Select(p => new PlayerWithSponsor
            {
                Id = p.Id,
                Name = p.ApprovedName,
                Sponsor = new SimpleSponsor
                {
                    Id = p.Sponsor.Id,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }
            })
        };
    }
}
