// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificateHtmlQuery(string ChallengeSpecId, string CertificateOwnerUserId, User ActingUser, string RequestedName) : IRequest<string>;

internal class GetPracticeModeCertificateHtmlHandler
(
    EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> certificateOwnerExists,
    ICertificatesService certificatesService,
    IPracticeService practiceService,
    IStore store,
    IValidatorService<GetPracticeModeCertificateHtmlQuery> validatorService
) : IRequestHandler<GetPracticeModeCertificateHtmlQuery, string>
{
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> _certificateOwnerExists = certificateOwnerExists;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IStore _store = store;
    private readonly IValidatorService<GetPracticeModeCertificateHtmlQuery> _validatorService = validatorService;

    public async Task<string> Handle(GetPracticeModeCertificateHtmlQuery request, CancellationToken cancellationToken)
    {
        var userPracticeCertificates = await _certificatesService
            .GetPracticeCertificates(request.CertificateOwnerUserId, cancellationToken);
        var certificate = userPracticeCertificates
            .OrderByDescending(c => c.Date)
            .FirstOrDefault(c => c.Challenge.ChallengeSpecId == request.ChallengeSpecId);

        await _validatorService
            .AddValidator(_certificateOwnerExists.UseProperty(r => r.CertificateOwnerUserId))
            .AddValidator((request, context) =>
            {
                if (request.CertificateOwnerUserId != request.ActingUser.Id && certificate.PublishedOn is null)
                    context.AddValidationException(new CertificateIsntPublished(request.CertificateOwnerUserId, PublishedCertificateMode.Practice, request.ChallengeSpecId));

                return Task.CompletedTask;
            })
            .Validate(request, cancellationToken);

        if (certificate is null)
        {
            throw new ResourceNotFound<PublishedPracticeCertificate>(request.ChallengeSpecId, $"Couldn't resolve a certificate for owner {request.CertificateOwnerUserId} and challenge spec {request.ChallengeSpecId}");
        }

        // first check the game for its certificate template
        var templateId = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == certificate.Game.Id)
            .Select(g => g.PracticeCertificateTemplateId)
            .SingleOrDefaultAsync(cancellationToken);

        if (templateId.IsEmpty())
        {
            // if the game doesn't have one, the global practice settings might
            var settings = await _practiceService.GetSettings(cancellationToken);

            templateId = settings.CertificateTemplateId;
        }

        if (templateId.IsEmpty())
        {
            throw new NoCertificateTemplateConfigured(request.ChallengeSpecId);
        }

        var certificateContext = new CertificateHtmlContext
        {
            Challenge = new CertificateHtmlContextChallenge
            {
                Id = certificate.Challenge.Id,
                Name = certificate.Challenge.Name,
                Description = certificate.Challenge.Description
            },
            Game = new CertificateHtmlContextGame
            {
                Id = certificate.Game.Id,
                Name = certificate.Game.Name,
                Division = certificate.Game.Division,
                Season = certificate.Game.Season,
                Series = certificate.Game.Series,
                Track = certificate.Game.Track
            },
            Date = certificate.Date,
            Duration = certificate.Time,
            PlayerName = certificate.PlayerName,
            Score = certificate.Score,
            TeamName = certificate.TeamName,
            UserId = request.ActingUser.Id,
            UserName = certificate.UserName,
            UserRequestedName = request.RequestedName
        };

        return await _certificatesService.BuildCertificateHtml(templateId, certificateContext, cancellationToken);
    }
}
