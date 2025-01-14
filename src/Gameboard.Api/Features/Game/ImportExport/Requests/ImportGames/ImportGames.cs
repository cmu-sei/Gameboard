using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record ImportGamesCommand(byte[] ImportPackage) : IRequest<ImportedGame[]>;

internal sealed class ImportGamesHandler
(
    IActingUserService actingUser,
    CoreOptions coreOptions,
    IGameImportExportService importExportService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<ImportGamesCommand, ImportedGame[]>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IGameImportExportService _importExportService = importExportService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<ImportedGame[]> Handle(ImportGamesCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequirePermissions(PermissionKey.Games_CreateEditDelete))
            .AddValidator(ctx =>
            {
                if (request.ImportPackage.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<byte[]>(nameof(request.ImportPackage)));
                }
            })
            .Validate(cancellationToken);

        return await _importExportService.ImportPackage(request.ImportPackage, cancellationToken);
    }
}
