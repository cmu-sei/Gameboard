using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed record DeleteExternalGameHostCommand(string DeleteHostId, string ReplaceHostId) : IRequest;

internal sealed class DeleteExternalGameHostHandler : IRequestHandler<DeleteExternalGameHostCommand>
{
    private readonly EntityExistsValidator<DeleteExternalGameHostCommand, ExternalGameHost> _hostExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<DeleteExternalGameHostCommand> _validator;

    public DeleteExternalGameHostHandler
    (
        EntityExistsValidator<DeleteExternalGameHostCommand, ExternalGameHost> hostExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<DeleteExternalGameHostCommand> validator
    )
    {
        _hostExists = hostExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(DeleteExternalGameHostCommand request, CancellationToken cancellationToken)
    {
        // auth and validate
        _userRoleAuthorizer
            .AllowRoles(UserRole.Designer, UserRole.Admin)
            .Authorize();

        _validator.AddValidator((req, ctx) =>
        {
            if (req.DeleteHostId.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(req.DeleteHostId), req.DeleteHostId));
        });
        _validator.AddValidator(async (req, ctx) =>
        {
            var hosts = await _store
                .WithNoTracking<ExternalGameHost>()
                .Where(h => h.Id == req.DeleteHostId || h.Id == req.ReplaceHostId)
                .Select(h => new { h.Id, GameCount = h.UsedByGames.Count() })
                .ToArrayAsync(cancellationToken);

            var deleteHost = hosts.SingleOrDefault(host => host.Id == req.DeleteHostId);
            if (deleteHost is null)
                ctx.AddValidationException(new ResourceNotFound<ExternalGameHost>(req.DeleteHostId));

            // note that is also where we validate that replace host id is set
            if (deleteHost.GameCount > 0)
            {
                if (req.ReplaceHostId.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(req.ReplaceHostId), req.ReplaceHostId));
                else if (req.DeleteHostId == req.ReplaceHostId)
                    ctx.AddValidationException(new DeleteAndReplaceHostIdsMustBeDifferent(req.DeleteHostId));
            }
            if (deleteHost.GameCount > 0 && !hosts.Any(host => host.Id == req.ReplaceHostId))
                ctx.AddValidationException(new ResourceNotFound<ExternalGameHost>(req.ReplaceHostId));
        });
        await _validator.Validate(request, cancellationToken);

        // move all games using the host to delete to a different external host
        if (request.ReplaceHostId.IsNotEmpty())
            await _store
                .WithNoTracking<Data.Game>()
                .Where(g => g.ExternalHostId == request.DeleteHostId)
                .ExecuteUpdateAsync(up => up.SetProperty(g => g.ExternalHostId, request.ReplaceHostId), cancellationToken);

        // delete the host
        await _store
            .WithNoTracking<ExternalGameHost>()
            .Where(h => h.Id == request.DeleteHostId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
