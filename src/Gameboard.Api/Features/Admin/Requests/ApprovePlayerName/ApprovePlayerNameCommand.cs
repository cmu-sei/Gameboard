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

public record ApprovePlayerNameCommand(string PlayerId, string Name, string RevisionReason) : IRequest;

internal class ApprovePlayerNameHandler : IRequestHandler<ApprovePlayerNameCommand>
{
    private readonly EntityExistsValidator<ApprovePlayerNameCommand, Data.Player> _playerExists;
    private readonly IStore _store;
    private readonly IValidatorService<ApprovePlayerNameCommand> _validatorService;

    public ApprovePlayerNameHandler
    (
        EntityExistsValidator<ApprovePlayerNameCommand, Data.Player> playerExists,
        IStore store,
        ValidatorService<ApprovePlayerNameCommand> validatorService
    )
    {
        _playerExists = playerExists;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Handle(ApprovePlayerNameCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(config => config.RequirePermissions(UserRolePermissionKey.Players_ApproveNameChanges))
            .AddValidator(_playerExists.UseProperty(r => r.PlayerId))
            .AddValidator((req, ctx) => Task.FromResult(req.Name.IsNotEmpty() && req.Name.Length > 2))
            .Validate(request, cancellationToken);

        // and go!
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == request.PlayerId)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.ApprovedName, request.Name)
                    .SetProperty(p => p.Name, string.Empty)
                    .SetProperty(p => p.NameStatus, request.RevisionReason),
                cancellationToken
            );
    }
}
