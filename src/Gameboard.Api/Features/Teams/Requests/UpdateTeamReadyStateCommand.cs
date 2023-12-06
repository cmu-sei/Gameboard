using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record UpdateTeamReadyStateCommand(string TeamId, bool IsReady) : IRequest;

internal class UpdateTeamReadyStateHandler : IRequestHandler<UpdateTeamReadyStateCommand>
{
    private readonly ISyncStartGameService _syncStartService;
    private readonly TeamExistsValidator<UpdateTeamReadyStateCommand> _teamExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<UpdateTeamReadyStateCommand> _validatorService;

    public UpdateTeamReadyStateHandler
    (
        ISyncStartGameService syncStartService,
        TeamExistsValidator<UpdateTeamReadyStateCommand> teamExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<UpdateTeamReadyStateCommand> validatorService
    )
    {
        _syncStartService = syncStartService;
        _teamExists = teamExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Handle(UpdateTeamReadyStateCommand request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

        await _validatorService
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .Validate(request, cancellationToken);

        await _syncStartService.UpdateTeamReadyState(request.TeamId, request.IsReady, cancellationToken);
    }
}
