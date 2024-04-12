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

            if (req.ReplaceHostId.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(req.ReplaceHostId), req.ReplaceHostId));
        });
        _validator.AddValidator(async (req, ctx) =>
        {
            var hosts = await _store
                .WithNoTracking<ExternalGameHost>()
                .Where(h => h.Id == req.DeleteHostId || h.Id == req.ReplaceHostId)
                .Select(h => h.Id)
                .ToArrayAsync(cancellationToken);

            if (!hosts.Any(hId => hId == req.DeleteHostId))
                ctx.AddValidationException(new ResourceNotFound<ExternalGameHost>(req.DeleteHostId));

            if (!hosts.Any(hId => hId == req.ReplaceHostId))
                ctx.AddValidationException(new ResourceNotFound<ExternalGameHost>(req.ReplaceHostId));
        });
        await _validator.Validate(request, cancellationToken);

        // move all games using the host to delete to a different external host
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
