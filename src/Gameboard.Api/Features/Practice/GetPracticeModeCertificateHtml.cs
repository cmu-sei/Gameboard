using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificateHtmlQuery(string ChallengeSpecId, User ActingUser) : IRequest<string>;

internal class GetPracticeModeCertificateHtmlHandler : IRequestHandler<GetPracticeModeCertificateHtmlQuery, string>
{
    private readonly IPracticeService _practiceService;
    private readonly EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> _userExists;
    private readonly IValidatorService<GetPracticeModeCertificateHtmlQuery> _validatorService;

    public GetPracticeModeCertificateHtmlHandler
    (
        IPracticeService practiceService,
        EntityExistsValidator<GetPracticeModeCertificateHtmlQuery, Data.User> userExists,
        IValidatorService<GetPracticeModeCertificateHtmlQuery> validatorService
    )
    {
        _practiceService = practiceService;
        _userExists = userExists;
        _validatorService = validatorService;
    }

    public async Task<string> Handle(GetPracticeModeCertificateHtmlQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .AddValidator(_userExists.UseProperty(r => r.ActingUser.Id))
            .Validate(request);

        var certificate = (await _practiceService.GetCertificates(request.ActingUser.Id))
            .FirstOrDefault(c => c.Challenge.ChallengeSpecId == request.ChallengeSpecId);

        if (certificate is null)
            return null;

        var settings = await _practiceService.GetSettings(cancellationToken);
        if (settings.CertificateHtmlTemplate.IsEmpty())
            return
            $"""
                <p>
                    You successfully completed challenge {certificate.Challenge.Name} on {certificate.Date} with
                    a score of {certificate.Score} and a time of {certificate.Time}, but the administrator of this
                    site hasn't configured a certificate template for Practice Mode.
                </p>
            """.Trim();

        return settings.CertificateHtmlTemplate
            .Replace("{{challengeName}}", certificate.Challenge.Name)
            .Replace("{{challengeDescription}}", certificate.Challenge.Description)
            .Replace("{{date}}", certificate.Date.ToLocalTime().ToString("M/d/yyyy"))
            .Replace("{{division}}", certificate.Game.Division)
            .Replace("{{playerName}}", certificate.PlayerName)
            .Replace("{{score}}", certificate.Score.ToString())
            .Replace("{{season}}", certificate.Game.Season)
            .Replace("{{time}}", GetDurationDescription(certificate.Time))
            .Replace("{{track}}", certificate.Game.Track);
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
