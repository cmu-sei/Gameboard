using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Teams;

public record AddToTeamCommand(string TeamId, string UserId) : IRequest<AddToTeamResponse>;

internal sealed class AddToTeamCommandHandler
(
    IActingUserService actingUser,
    PlayerService playerService,
    IStore store,
    ITeamService teamService,
    IValidatorService validator
) : IRequestHandler<AddToTeamCommand, AddToTeamResponse>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly PlayerService _playerService = playerService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validator;

    public async Task<AddToTeamResponse> Handle(AddToTeamCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth
            (
                c => c
                    .Require(Users.PermissionKey.Teams_Enroll)
                    .Unless
                    (
                        async () => await _store
                            .WithNoTracking<Data.Player>()
                            .Where(p => p.TeamId == request.TeamId && p.Role == PlayerRole.Manager)
                            .Where(p => p.UserId == _actingUser.Get().Id)
                            .AnyAsync(cancellationToken)
                    )
            )
            .AddValidator(async ctx =>
            {
                // team hasn't started playing
                var team = await _teamService.GetTeam(request.TeamId);
                if (team.SessionBegin.IsNotEmpty())
                {
                    ctx.AddValidationException(new SessionAlreadyStarted(request.TeamId));
                }

                // team's current roster has to be < max
                var gameId = await _teamService.GetGameId(request.TeamId, cancellationToken);
                var maxTeamSize = await _store
                    .WithNoTracking<Data.Game>()
                    .Where(g => g.Id == gameId)
                    .Select(g => g.MaxTeamSize)
                    .SingleAsync(cancellationToken);

                if (team.Members.Count() >= maxTeamSize)
                {
                    ctx.AddValidationException(new TeamIsFull(new SimpleEntity { Id = team.TeamId, Name = team.ApprovedName }, team.Members.Count(), maxTeamSize));
                }

                // if the player is joining a competitive team, they can't have played this game
                // competitively before
                if (team.Mode == PlayerMode.Competition)
                {
                    var priorPlayer = await _store
                        .WithNoTracking<Data.Player>()
                        .Where(p => p.UserId == request.UserId && p.TeamId != team.TeamId)
                        .Where(p => p.GameId == team.GameId)
                        .Where(p => p.Mode == PlayerMode.Competition)
                        .WhereDateIsNotEmpty(p => p.SessionBegin)
                        .Select(p => new
                        {
                            p.Id,
                            p.ApprovedName,
                            Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Name },
                            User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName },
                            p.SessionBegin,
                            p.TeamId
                        })
                        .SingleOrDefaultAsync(cancellationToken);

                    if (priorPlayer is not null)
                    {
                        ctx.AddValidationException(new UserAlreadyPlayed(priorPlayer.User, priorPlayer.Game, priorPlayer.TeamId, priorPlayer.SessionBegin));
                    }
                }
            })
            // 
            .Validate(cancellationToken);

        // first find the team they're meant to join
        var team = await _teamService.GetTeam(request.TeamId);

        // first ensure the person is enrolled
        var existingPlayerId = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.UserId == request.UserId)
            .Where(p => p.TeamId != request.TeamId)
            .WhereDateIsEmpty(p => p.SessionBegin)
            .Where(p => p.GameId == team.GameId)
            .Select(p => p.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingPlayerId.IsEmpty())
        {
            var existingPlayer = await _playerService.Enroll
            (
                new NewPlayer { GameId = team.GameId, UserId = request.UserId },
                _actingUser.Get(),
                cancellationToken
            );

            existingPlayerId = existingPlayer.Id;
        }

        var players = await _teamService.AddPlayers(request.TeamId, cancellationToken, existingPlayerId);
        var addedPlayer = players.Single();

        return new AddToTeamResponse
        {
            Game = new SimpleEntity { Id = addedPlayer.GameId, Name = addedPlayer.GameName },
            Player = new SimpleEntity { Id = addedPlayer.Id, Name = addedPlayer.ApprovedName },
            Team = new SimpleEntity { Id = team.TeamId, Name = team.ApprovedName },
            User = new SimpleEntity { Id = addedPlayer.UserId, Name = addedPlayer.UserApprovedName }
        };
    }
}
