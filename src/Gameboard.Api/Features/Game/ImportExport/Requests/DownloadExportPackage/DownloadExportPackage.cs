// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record DownloadExportPackageRequest(string ExportBatchId) : IRequest<byte[]>;

internal sealed class DownloadExportPackageHandler
(
    IGameImportExportService importExport,
    IValidatorService validator
) : IRequestHandler<DownloadExportPackageRequest, byte[]>
{
    private readonly IGameImportExportService _importExport = importExport;
    private readonly IValidatorService _validator = validator;

    public async Task<byte[]> Handle(DownloadExportPackageRequest request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        return await _importExport.GetExportedPackageContent(request.ExportBatchId, cancellationToken);
    }
}
