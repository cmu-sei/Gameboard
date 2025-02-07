using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record DeleteExportBatchCommand(string ExportBatchId) : IRequest;

internal sealed class DeleteExportBatchHandler
(
    IGameImportExportService importExport,
    IValidatorService validator
) : IRequestHandler<DeleteExportBatchCommand>
{
    private readonly IGameImportExportService _importExport = importExport;
    private readonly IValidatorService _validator = validator;

    public async Task Handle(DeleteExportBatchCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequireOneOf(PermissionKey.Games_CreateEditDelete))
            .AddEntityExistsValidator<GameExportBatch>(request.ExportBatchId)
            .Validate(cancellationToken);

        await _importExport.DeleteExportPackage(request.ExportBatchId, cancellationToken);
    }
}
