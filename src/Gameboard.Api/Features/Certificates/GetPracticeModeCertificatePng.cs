using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificatePngQuery(string ChallengeSpecId, string CertificateOwnerUserId, User ActingUser) : IRequest<string>;

internal class GetPracticeModeCertificatePngHandler : IRequestHandler<GetPracticeModeCertificatePngQuery, string>
{
    private readonly CoreOptions _coreOptions;
    private readonly IPracticeService _practiceService;
    private readonly EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User> _actingUserExists;
    private readonly EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User> _certificateOwnerExists;
    private readonly IValidatorService<GetPracticeModeCertificatePngQuery> _validatorService;

    public GetPracticeModeCertificatePngHandler
    (
        CoreOptions coreOptions,
        IPracticeService practiceService,
        EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User> certificateOwnerExists,
        EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User> actingUserExists,
        IValidatorService<GetPracticeModeCertificatePngQuery> validatorService
    )
    {
        _actingUserExists = actingUserExists;
        _certificateOwnerExists = certificateOwnerExists;
        _coreOptions = coreOptions;
        _practiceService = practiceService;
        _validatorService = validatorService;
    }

    public async Task<string> Handle(GetPracticeModeCertificatePngQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .AddValidator(_actingUserExists.UseProperty(r => r.ActingUser.Id))
            .AddValidator(_certificateOwnerExists.UseProperty(r => r.CertificateOwnerUserId))
            .Validate(request);

        // tODO: validate publication if the owner isn't the actor

        var certificate = (await _practiceService.GetCertificates(request.CertificateOwnerUserId))
            .FirstOrDefault(c => c.Challenge.ChallengeSpecId == request.ChallengeSpecId);

        if (certificate is null)
            return null;

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
                site hasn't configured a certificate template for Practice Mode.
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
