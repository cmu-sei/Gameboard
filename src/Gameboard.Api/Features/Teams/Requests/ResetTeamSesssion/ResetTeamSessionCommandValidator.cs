using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Player;

internal class ResetSessionCommandValidator : IGameboardRequestValidator<ResetTeamSessionCommand>
{
    private readonly PlayerService _playerService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<ResetTeamSessionCommand> _teamExistsValidator;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<ResetTeamSessionCommand> _validatorService;

    public ResetSessionCommandValidator
    (
        PlayerService playerService,
        IStore store,
        TeamExistsValidator<ResetTeamSessionCommand> teamExistsValidator,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<ResetTeamSessionCommand> validatorService
    )
    {
        _playerService = playerService;
        _store = store;
        _teamExistsValidator = teamExistsValidator;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Validate(ResetTeamSessionCommand request, CancellationToken cancellationToken)
    {
        // get the game first - we need it to know if we need to update the sync start state later,
        // and we need to validate that it can be reset
        var players = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Game)
            .Where(p => p.TeamId == request.TeamId)
            .ToArrayAsync(cancellationToken);

        // we can assume a single game because the team validator below will throw if there's more than
        // one
        var game = players.Select(p => p.Game).DistinctBy(g => g.Id).FirstOrDefault();

        // users with a correct role can do this, but those without can only do it if they're on the
        // team being reset AND if the game allows reset AND if no players on the team have
        // started a session
        _userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Tester, UserRole.Support);
        if (!_userRoleAuthorizer.WouldAuthorize())
        {
            if (!players.Any(p => p.UserId == request.ActingUser.Id))
                throw new UserIsntOnTeam(request.ActingUser.Id, request.TeamId, $"""Users without an elevated role can't reset a team session unless they're on the team.""");

            var hasStartedSession = players.Any(p => p.SessionBegin > DateTimeOffset.MinValue);
            if (!game.AllowReset || !hasStartedSession)
                throw new GameDoesntAllowReset(game.Id);
        }

        _validatorService.AddValidator(_teamExistsValidator.UseProperty(r => r.TeamId));
        await _validatorService.Validate(request, cancellationToken);
    }
}
