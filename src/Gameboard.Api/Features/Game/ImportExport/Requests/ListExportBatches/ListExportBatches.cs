// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record ListExportBatchesQuery() : IRequest<ListExportBatchesResponse>;

internal sealed class ListExportBatchesHandler(IAppUrlService appUrl, IStore store, IValidatorService validator) : IRequestHandler<ListExportBatchesQuery, ListExportBatchesResponse>
{
    private readonly IAppUrlService _appUrl = appUrl;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<ListExportBatchesResponse> Handle(ListExportBatchesQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequireOneOf(PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        var batches = await _store
            .WithNoTracking<GameExportBatch>()
            .Select(b => new GameExportBatchView
            {
                Id = b.Id,
                ExportedBy = new SimpleEntity { Id = b.ExportedByUserId, Name = b.ExportedByUser.ApprovedName },
                ExportedOn = b.ExportedOn,
                GameCount = b.IncludedGames.Count(),
                PackageDownloadUrl = _appUrl.ToAppAbsoluteUrl($"api/games/export-batches/{b.Id}")
            })
            .OrderByDescending(b => b.ExportedOn)
            .ToArrayAsync(cancellationToken);

        return new ListExportBatchesResponse { ExportBatches = batches };
    }
}
