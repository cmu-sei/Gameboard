using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record ExportGameCommand(string[] GameIds, bool? IncludePracticeAreaDefaultCertificateTemplate) : IRequest<ExportGamesResult>;

internal sealed class ExportGameHandler
(
    IGameImportExportService importExportService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<ExportGameCommand, ExportGamesResult>
{
    private readonly IGameImportExportService _importExportService = importExportService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<ExportGamesResult> Handle(ExportGameCommand request, CancellationToken cancellationToken)
    {
        var finalGameIds = Array.Empty<string>();
        if (request.GameIds is not null)
        {
            finalGameIds = request.GameIds.Distinct().Where(gId => gId.IsNotEmpty()).ToArray();
        }

        await _validator
            .Auth(c => c.RequirePermissions(Users.PermissionKey.Games_CreateEditDelete))
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

        var batch = await _importExportService.ExportGames
        (
            request.GameIds,
            request.IncludePracticeAreaDefaultCertificateTemplate.GetValueOrDefault(),
            cancellationToken
        );

        return new ExportGamesResult { ExportBatch = batch };
    }
}
