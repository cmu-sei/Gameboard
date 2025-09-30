// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games.ImportExport;

public record PreviewImportPackageQuery(byte[] Package) : IRequest<GameImportExportBatch>;

internal sealed class PreviewImportPackageHandler(IGameImportExportService gameImportExportService, IValidatorService validator) : IRequestHandler<PreviewImportPackageQuery, GameImportExportBatch>
{
    public async Task<GameImportExportBatch> Handle(PreviewImportPackageQuery request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        return await gameImportExportService.PreviewImportPackage(request.Package, cancellationToken);
    }
}
