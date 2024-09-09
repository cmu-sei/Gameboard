using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record AddPlayerToTeamCommand(string PlayerId, string InvitationCode) : IRequest<Api.Player>;

internal class AddPlayerToTeamHandler(
    IActingUserService actingUserService,
    IInternalHubBus hubBus,
    IMapper mapper,
    IMediator mediator,
    INowService nowService,
    IUserRolePermissionsService permissionsService,
    EntityExistsValidator<Data.Player> playerExists,
    IStore store,
    ITeamService teamService,
    IValidatorService<AddPlayerToTeamCommand> validatorService) : IRequestHandler<AddPlayerToTeamCommand, Api.Player>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IInternalHubBus _hubBus = hubBus;
    private readonly IMapper _mapper = mapper;
    private readonly IMediator _mediator = mediator;
    private readonly INowService _now = nowService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly EntityExistsValidator<Data.Player> _playerExists = playerExists;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<AddPlayerToTeamCommand> _validatorService = validatorService;

    public async Task<Api.Player> Handle(AddPlayerToTeamCommand request, CancellationToken cancellationToken)
    {
        var code = request.InvitationCode.Trim();
        ArgumentException.ThrowIfNullOrEmpty(code);

        // auth/validate
        await _validatorService
            .Auth
            (
                config => config
                    .RequirePermissions(PermissionKey.Teams_Enroll)
                    .Unless(() => _store.AnyAsync<Data.Player>(p => p.UserId == _actingUserService.Get().Id && p.Id == request.PlayerId, cancellationToken))
            )
            .AddValidator(_playerExists.WithIdValue(request.PlayerId))
            .AddValidator(async (req, ctx) =>
            {
                var teamPlayers = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.InviteCode == req.InvitationCode)
                    .Select(p => new
                    {
                        p.Id,
                        p.Role,
                        p.Sponsor,
                        p.TeamId,
                        Game = new
                        {
                            Id = p.GameId,
                            p.Game.MaxTeamSize,
                            p.Game.RegistrationClose,
                            p.Game.RegistrationOpen,
                            p.Game.RequireSponsoredTeam
                        }
                    })
                    .ToArrayAsync(cancellationToken);

                var teamIds = teamPlayers.Select(p => p.TeamId).Distinct().ToArray();
                if (teamIds.Length != 1)
                {
                    ctx.AddValidationException(new CantResolveTeamFromCode(req.InvitationCode, teamIds));
                    return;
                }

                var games = teamPlayers.Select(p => p.Game).Distinct().ToArray();
                if (games.Length > 1)
                {
                    ctx.AddValidationException(new PlayersAreInMultipleGames(games.Select(g => g.Id)));
                    return;
                }
                var game = games[0];

                var toEnroll = await _store
                    .WithNoTracking<Data.Player>()
                    .Select(p => new
                    {
                        p.Id,
                        p.GameId,
                        p.Sponsor
                    })
                    .Where(p => p.Id == req.PlayerId)
                    .SingleAsync(cancellationToken);

                if (game.Id != toEnroll.GameId)
                {
                    ctx.AddValidationException(new NotYetRegistered(toEnroll.Id, game.Id));
                    return;
                }

                var canIgnoreRegistrationWindow = await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow);
                var invitingPlayer = teamPlayers.Where(p => p.Role == PlayerRole.Manager).SingleOrDefault() ?? teamPlayers.First();
                if (!canIgnoreRegistrationWindow)
                {
                    var nowish = _now.Get();

                    if (nowish < game.RegistrationOpen || (nowish > game.RegistrationClose && game.RegistrationClose.IsNotEmpty()))
                        ctx.AddValidationException(new RegistrationIsClosed(game.Id, "Can't join the team because the game's registration is closed."));

                    if (game.RequireSponsoredTeam && !teamPlayers.Any(p => p.Sponsor.Id == toEnroll.Sponsor.Id))
                    {
                        var teamSponsor = teamPlayers.Select(p => p.Sponsor).Distinct().Single();
                        ctx.AddValidationException(new RequiresSameSponsor(game.Id, invitingPlayer.Id, teamSponsor.Name, req.PlayerId, toEnroll.Sponsor.Name));
                    }
                }

                if (teamPlayers.Length >= game.MaxTeamSize)
                    ctx.AddValidationException(new TeamIsFull(invitingPlayer.Id, teamPlayers.Length, game.MaxTeamSize));
            })
            .Validate(request, cancellationToken);

        // actually do the thing
        var teamId = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.InviteCode == code)
            .Select(p => p.TeamId)
            .SingleAsync(cancellationToken);

        var addedPlayers = await _teamService.AddPlayers(teamId, cancellationToken, request.PlayerId);
        if (addedPlayers.Count() == 1)
            return addedPlayers.Single();

        throw new Exception($"An error occurred: {addedPlayers.Count()} players added");
    }
}
