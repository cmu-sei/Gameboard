using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificateHtmlQuery(string ChallengeSpecId, string CertificateOwnerUserId, User ActingUser) : IRequest<string>;

internal class GetPracticeModeCertificateHtmlHandler : IRequestHandler<GetPracticeModeCertificateHtmlQuery, string>
{
    private readonly ICertificatesService _certificatesService;
    private readonly CoreOptions _coreOptions;
    private readonly EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> _actingUserExists;
    private readonly EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> _certificateOwnerExists;
    private readonly IPracticeService _practiceService;
    private readonly IValidatorService<GetPracticeModeCertificateHtmlQuery> _validatorService;

    public GetPracticeModeCertificateHtmlHandler
    (
        EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> actingUserExists,
        EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> certificateOwnerExists,
        ICertificatesService certificatesService,
        CoreOptions coreOptions,
        IPracticeService practiceService,
        IValidatorService<GetPracticeModeCertificateHtmlQuery> validatorService
    )
    {
        _actingUserExists = actingUserExists;
        _certificateOwnerExists = certificateOwnerExists;
        _certificatesService = certificatesService;
        _coreOptions = coreOptions;
        _practiceService = practiceService;
        _validatorService = validatorService;
    }

    public async Task<string> Handle(GetPracticeModeCertificateHtmlQuery request, CancellationToken cancellationToken)
    {
        var certificate = (await _certificatesService.GetPracticeCertificates(request.CertificateOwnerUserId))
                    .FirstOrDefault(c => c.Challenge.ChallengeSpecId == request.ChallengeSpecId);

        await _validatorService
            .AddValidator(_actingUserExists.UseProperty(r => r.ActingUser.Id))
            .AddValidator(_certificateOwnerExists.UseProperty(r => r.CertificateOwnerUserId))
            .AddValidator((request, context) =>
            {
                if (request.CertificateOwnerUserId != request.ActingUser.Id && certificate.PublishedOn is null)
                    context.AddValidationException(new CertificateIsntPublished(request.CertificateOwnerUserId, PublishedCertificateMode.Practice, request.ChallengeSpecId));

                return Task.CompletedTask;
            })
            .Validate(request, cancellationToken);

        if (certificate is null)
            throw new ResourceNotFound<PublishedPracticeCertificate>(request.ChallengeSpecId, $"Couldn't resolve a certificate for owner {request.CertificateOwnerUserId} and challenge spec {request.ChallengeSpecId}");

        // load the outer template from this application (this is custom crafted by us to ensure we end up)
        // with a consistent HTML-compliant base
        var outerTemplatePath = Path.Combine(_coreOptions.TemplatesDirectory, "practice-certificate.template.html");
        var outerTemplate = File.ReadAllText(outerTemplatePath);

        // the "inner" template is user-defined and loaded from settings
        var settings = await _practiceService.GetSettings(cancellationToken);
        var innerTemplate = $"""
            <p>
                You successfully completed challenge {certificate.Challenge.Name} on {certificate.Date} with
                a score of {certificate.Score} and a time of {certificate.Time}, but the administrator of this
                site hasn't configured a certificate template for the Practice Area.
            </p>
        """.Trim();

        if (!settings.CertificateHtmlTemplate.IsEmpty())
            innerTemplate = settings.CertificateHtmlTemplate
                .Replace("{{challengeName}}", certificate.Challenge.Name)
                .Replace("{{challengeDescription}}", certificate.Challenge.Description)
                .Replace("{{date}}", certificate.Date.ToLocalTime().ToString("M/d/yyyy"))
                .Replace("{{division}}", certificate.Game.Division)
                .Replace("{{playerName}}", certificate.PlayerName)
                .Replace("{{score}}", certificate.Score.ToString())
                .Replace("{{season}}", certificate.Game.Season)
                .Replace("{{time}}", GetDurationDescription(certificate.Time))
                .Replace("{{track}}", certificate.Game.Track);

        // compose final html and save to a temp file
        return outerTemplate.Replace("{{bodyContent}}", innerTemplate);
    }

    internal string GetDurationDescription(TimeSpan time)
    {
        // compute time string - hours and minutes rounded off
        var timeString = "Less than a minute";
        if (Math.Round(time.TotalMinutes, 0) > 0)
        {
            var hours = Math.Floor(time.TotalHours);

            // for each of the hour and minute strings, do pluralization stuff or set to empty
            // if the value is zero
            var hoursString = hours > 0 ? $"{hours} hour{(hours == 1 ? "" : "s")}" : string.Empty;
            var minutesString = time.Minutes > 0 ? $"{time.Minutes} minute{(time.Minutes == 1 ? "" : "s")}" : string.Empty;

            timeString = $"{hoursString}{(!hoursString.IsEmpty() && !minutesString.IsEmpty() ? " and " : "")}{minutesString}";
        }

        return timeString;
    }
}
