using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record UpdatePlayerNameChangeRequestCommand(string PlayerId, UpdatePlayerNameChangeRequestArgs Args) : IRequest;

internal class UpdatePlayerNameChangeRequestHandler
(
    EntityExistsValidator<UpdatePlayerNameChangeRequestCommand, Data.Player> playerExists,
    IStore store,
    ValidatorService<UpdatePlayerNameChangeRequestCommand> validatorService
) : IRequestHandler<UpdatePlayerNameChangeRequestCommand>
{
    private readonly EntityExistsValidator<UpdatePlayerNameChangeRequestCommand, Data.Player> _playerExists = playerExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<UpdatePlayerNameChangeRequestCommand> _validatorService = validatorService;

    public async Task Handle(UpdatePlayerNameChangeRequestCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config => config.RequirePermissions(PermissionKey.Teams_ApproveNameChanges))
            .AddValidator(_playerExists.UseProperty(r => r.PlayerId))
            .AddValidator((req, ctx) => Task.FromResult(req.Args.ApprovedName.IsNotEmpty() && req.Args.ApprovedName.Length > 2))
            .Validate(request, cancellationToken);

        // and go!
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == request.PlayerId)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.ApprovedName, request.Args.ApprovedName)
                    .SetProperty(p => p.Name, request.Args.RequestedName)
                    .SetProperty(p => p.NameStatus, request.Args.Status),
                cancellationToken
            );
    }
}
