// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record ExportGamesCommand(string[] GameIds, bool? IncludePracticeAreaDefaultCertificateTemplate) : IRequest<GameImportExportBatch>;

internal sealed class ExportGamesHandler
(
    IGameImportExportService importExportService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<ExportGamesCommand, GameImportExportBatch>
{
    private readonly IGameImportExportService _importExportService = importExportService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<GameImportExportBatch> Handle(ExportGamesCommand request, CancellationToken cancellationToken)
    {
        var finalGameIds = Array.Empty<string>();
        if (request.GameIds.IsNotEmpty())
        {
            finalGameIds = [.. request.GameIds.Distinct().Where(gId => gId.IsNotEmpty())];
        }

        await _validator
            .Auth(c => c.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddValidator(ctx =>
            {
                if (request.GameIds.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string[]>(nameof(request.GameIds)));
                }
            })
            .AddValidator(async ctx =>
            {
                if ((request?.GameIds?.Length ?? 0) == 0)
                {
                    return;
                }

                var gamesExist = await _store
                    .WithNoTracking<Data.Game>()
                    .Where(g => finalGameIds.Contains(g.Id))
                    .Select(g => g.Id)
                    .ToArrayAsync(cancellationToken);

                if (gamesExist.Length != request.GameIds.Length)
                {
                    foreach (var gameId in request.GameIds.Where(gId => !gamesExist.Contains(gId)))
                    {
                        ctx.AddValidationException(new ResourceNotFound<Data.Game>(gameId));
                    }
                }

            })
            .Validate(cancellationToken);

        // if no gameIds have been passed, give them everything
        if (finalGameIds.IsEmpty())
        {
            finalGameIds = await _store
                .WithNoTracking<Data.Game>()
                .Select(g => g.Id)
                .ToArrayAsync(cancellationToken);
        }

        var batch = await _importExportService.ExportPackage
        (
            request.GameIds,
            request.IncludePracticeAreaDefaultCertificateTemplate.GetValueOrDefault(),
            cancellationToken
        );

        return batch;
    }
}
