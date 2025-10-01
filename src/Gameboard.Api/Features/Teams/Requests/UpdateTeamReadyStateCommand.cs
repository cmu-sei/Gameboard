// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record UpdateTeamReadyStateCommand(string TeamId, bool IsReady) : IRequest;

internal class UpdateTeamReadyStateHandler(
    ISyncStartGameService syncStartService,
    TeamExistsValidator<UpdateTeamReadyStateCommand> teamExists,
    IValidatorService<UpdateTeamReadyStateCommand> validatorService
    ) : IRequestHandler<UpdateTeamReadyStateCommand>
{
    private readonly ISyncStartGameService _syncStartService = syncStartService;
    private readonly TeamExistsValidator<UpdateTeamReadyStateCommand> _teamExists = teamExists;
    private readonly IValidatorService<UpdateTeamReadyStateCommand> _validatorService = validatorService;

    public async Task Handle(UpdateTeamReadyStateCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.Require(Users.PermissionKey.Teams_SetSyncStartReady))
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .Validate(request, cancellationToken);

        await _syncStartService.UpdateTeamReadyState(request.TeamId, request.IsReady, cancellationToken);
    }
}
