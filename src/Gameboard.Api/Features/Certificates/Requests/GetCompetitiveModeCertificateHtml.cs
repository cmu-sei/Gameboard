using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record GetCompetitiveModeCertificateHtmlQuery(string GameId, string OwnerUserId, string ActingUserId) : IRequest<string>;

internal class GetCompetitiveModeCertificateHtmlHandler
(
    EntityExistsValidator<GetCompetitiveModeCertificateHtmlQuery, Data.Game> gameExists,
    PlayerService playerService,
    IStore store,
    IValidatorService<GetCompetitiveModeCertificateHtmlQuery> validatorService
) : IRequestHandler<GetCompetitiveModeCertificateHtmlQuery, string>
{
    private readonly EntityExistsValidator<GetCompetitiveModeCertificateHtmlQuery, Data.Game> _gameExists = gameExists;
    private readonly PlayerService _playerService = playerService;
    private readonly IStore _store = store;
    private readonly IValidatorService<GetCompetitiveModeCertificateHtmlQuery> _validatorService = validatorService;

    public async Task<string> Handle(GetCompetitiveModeCertificateHtmlQuery request, CancellationToken cancellationToken)
    {
        var isPublished = await _store
            .WithNoTracking<PublishedCompetitiveCertificate>()
            .AnyAsync(c => c.GameId == request.GameId && c.OwnerUserId == request.OwnerUserId, cancellationToken);

        await _validatorService
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator((request, context) =>
            {
                if (request.OwnerUserId != request.ActingUserId && !isPublished)
                    context.AddValidationException(new CertificateIsntPublished(request.OwnerUserId, PublishedCertificateMode.Competitive, request.GameId));

                return Task.CompletedTask;
            })
            .Validate(request, cancellationToken);

        var player = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.UserId == request.OwnerUserId)
            .Where(p => p.GameId == request.GameId)
            .FirstAsync(cancellationToken);

        var certificate = await _playerService.MakeCertificate(player.Id);
        return certificate.Html;
    }
}
