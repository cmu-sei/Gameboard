using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record ExportGameCommand(string GameId) : IRequest<ExportGameResult>;

internal sealed class ExportGameHandler : IRequestHandler<ExportGameCommand, ExportGameResult>
{
    public Task<ExportGameResult> Handle(ExportGameCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
