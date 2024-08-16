using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Player;

internal class ResetSessionCommandValidator : IGameboardRequestValidator<ResetTeamSessionCommand>
{
    private readonly IStore _store;
    private readonly TeamExistsValidator<ResetTeamSessionCommand> _teamExistsValidator;
    private readonly IValidatorService<ResetTeamSessionCommand> _validatorService;

    public ResetSessionCommandValidator
    (
        IStore store,
        TeamExistsValidator<ResetTeamSessionCommand> teamExistsValidator,
        IValidatorService<ResetTeamSessionCommand> validatorService
    )
    {
        _store = store;
        _teamExistsValidator = teamExistsValidator;
        _validatorService = validatorService;
    }

    public async Task Validate(ResetTeamSessionCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(config =>
            {
                config
                    .RequirePermissions(UserRolePermissionKey.Play_IgnoreSessionResetSettings)
                    .Unless
                    (
                        () => _store
                            .WithNoTracking<Data.Player>()
                            .Where(p => p.TeamId == request.TeamId)
                            .Where(p => p.UserId == request.ActingUser.Id)
                            .Where(p => p.Game.AllowReset)
                            .AnyAsync(),
                        new UserIsntOnTeam(request.ActingUser.Id, request.TeamId, $"""Users without the {nameof(UserRolePermissionKey.Play_IgnoreSessionResetSettings)} can't reset a team session unless they're on the team.""")
                    );
            })
            .AddValidator(_teamExistsValidator.UseProperty(r => r.TeamId))
            .Validate(request, cancellationToken);
    }
}
