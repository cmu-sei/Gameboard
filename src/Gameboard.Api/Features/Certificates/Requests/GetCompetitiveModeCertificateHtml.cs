using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record GetCompetitiveModeCertificateHtmlQuery(string GameId, string OwnerUserId, string RequestedName) : IRequest<string>;

internal class GetCompetitiveModeCertificateHtmlHandler
(
    IActingUserService actingUserService,
    ICertificatesService certificatesService,
    EntityExistsValidator<GetCompetitiveModeCertificateHtmlQuery, Data.Game> gameExists,
    IStore store,
    IUserRolePermissionsService rolePermissions,
    IValidatorService<GetCompetitiveModeCertificateHtmlQuery> validatorService
) : IRequestHandler<GetCompetitiveModeCertificateHtmlQuery, string>
{
    private readonly IActingUserService _actingUser = actingUserService;
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly EntityExistsValidator<GetCompetitiveModeCertificateHtmlQuery, Data.Game> _gameExists = gameExists;
    private readonly IStore _store = store;
    private readonly IUserRolePermissionsService _rolePermissions = rolePermissions;
    private readonly IValidatorService<GetCompetitiveModeCertificateHtmlQuery> _validatorService = validatorService;

    public async Task<string> Handle(GetCompetitiveModeCertificateHtmlQuery request, CancellationToken cancellationToken)
    {
        var isPublished = await _store
            .WithNoTracking<PublishedCompetitiveCertificate>()
            .AnyAsync(c => c.GameId == request.GameId && c.OwnerUserId == request.OwnerUserId, cancellationToken);

        var templateId = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == request.GameId)
            .Select(g => g.CertificateTemplateId)
            .SingleOrDefaultAsync(cancellationToken);

        await _validatorService
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator(async (request, context) =>
            {
                var actingUser = _actingUser.Get();
                var hasPermission = await _rolePermissions.Can(PermissionKey.Admin_View);

                if (request.OwnerUserId != _actingUser.Get().Id && !isPublished && !hasPermission)
                    context.AddValidationException(new CertificateIsntPublished(request.OwnerUserId, PublishedCertificateMode.Competitive, request.GameId));
            })
            .AddValidator((req, ctx) =>
            {
                if (templateId.IsEmpty())
                {
                    ctx.AddValidationException(new NoCertificateTemplateConfigured(request.GameId));
                }
            })
            .Validate(request, cancellationToken);

        var userCompetitiveCertificates = await _certificatesService
            .GetCompetitiveCertificates(request.OwnerUserId, cancellationToken);
        var certificate = userCompetitiveCertificates
            .Where(c => c.Game.Id == request.GameId)
            .OrderByDescending(c => c.Date)
            .FirstOrDefault();

        return await _certificatesService.BuildCertificateHtml
        (
            templateId,
            new CertificateHtmlContext
            {
                Game = new CertificateHtmlContextGame
                {
                    Id = certificate.Game.Id,
                    Name = certificate.Game.Name,
                    Division = certificate.Game.Division,
                    Season = certificate.Game.Season,
                    Series = certificate.Game.Series,
                    Track = certificate.Game.Track,
                },
                Date = certificate.Date,
                Duration = certificate.Duration,
                PlayerName = certificate.PlayerName,
                Rank = certificate.Rank,
                Score = certificate.Score,
                TeamName = certificate.TeamName,
                TotalPlayerCount = certificate.UniquePlayerCount,
                TotalTeamCount = certificate.UniqueTeamCount,
                UserId = _actingUser.Get().Id,
                UserName = certificate.UserName,
                UserRequestedName = request.RequestedName
            },
            cancellationToken
        );
    }
}
