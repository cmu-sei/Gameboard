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

internal class AdminEnrollTeamHandler
(
    IActingUserService actingUserService,
    IGuidService guids,
    PlayerService playerService,
    IStore store,
    ITeamService teamService,
    IGameboardRequestValidator<AdminEnrollTeamRequest> validator
) : IRequestHandler<AdminEnrollTeamRequest, AdminEnrollTeamResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IGuidService _guids = guids;
    private readonly PlayerService _playerService = playerService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IGameboardRequestValidator<AdminEnrollTeamRequest> _validator = validator;

    public async Task<AdminEnrollTeamResponse> Handle(AdminEnrollTeamRequest request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _validator.Validate(request, cancellationToken);

        // get the acting admin
        var actingUser = _actingUserService.Get();

        // enlist all and retain the ids
        var teamUpCode = _guids.Generate();
        var createdPlayers = new List<Api.Player>();
        Api.Player captainPlayer = null;

        foreach (var userId in request.UserIds)
        {
            var newPlayer = await _playerService.Enroll(new NewPlayer { GameId = request.GameId, UserId = userId }, actingUser, cancellationToken);
            createdPlayers.Add(newPlayer);

            // if this is the captain (or if one wasn't specified and we haven't already randomly selected one), make them a team-up code
            if ((request.CaptainUserId.IsNotEmpty() && userId == request.CaptainUserId) || (captainPlayer is null && request.CaptainUserId.IsEmpty()))
            {
                captainPlayer = newPlayer;

                await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.Id == captainPlayer.Id)
                    .ExecuteUpdateAsync
                    (
                        up => up
                            .SetProperty(p => p.InviteCode, teamUpCode)
                            .SetProperty(p => p.Role, PlayerRole.Manager)
                        , cancellationToken
                    );
            }
        }

        // team everyone up
        // TODO: kinda yucky. Want to share logic about what it means to be added to a team, but all the validation around that
        // is in teamService
        var playersToAdd = createdPlayers.Where(p => p.Id != captainPlayer.Id).Select(p => p.Id).ToArray();
        if (playersToAdd.Length > 0)
        {
            await _teamService.AddPlayers(captainPlayer.TeamId, cancellationToken, playersToAdd);
        }

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
