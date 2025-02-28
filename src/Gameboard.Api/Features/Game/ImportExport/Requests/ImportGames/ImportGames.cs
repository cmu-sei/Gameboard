using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record ImportGamesCommand(byte[] ImportPackage, string[] GameIds, bool? SetGamesPublishStatus) : IRequest<ImportedGame[]>;

internal sealed class ImportGamesHandler
(
    IGameImportExportService importExportService,
    IValidatorService validator
) : IRequestHandler<ImportGamesCommand, ImportedGame[]>
{
    private readonly IGameImportExportService _importExportService = importExportService;
    private readonly IValidatorService _validator = validator;

    public async Task<ImportedGame[]> Handle(ImportGamesCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Games_CreateEditDelete))
            .AddValidator(ctx =>
            {
                if (request.ImportPackage.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<byte[]>(nameof(request.ImportPackage)));
                }
            })
            .Validate(cancellationToken);

        return await _importExportService.ImportPackage(request.ImportPackage, request.GameIds, request.SetGamesPublishStatus, cancellationToken);
    }
}
