// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public interface ICertificatesService
{
    Task<string> BuildCertificateHtml(string templateId, CertificateHtmlContext htmlContext, CancellationToken cancellationToken);
    Task<string> BuildCertificateTemplatePreviewHtml(string templateId, CancellationToken cancellationToken);
    IQueryable<CertificateTemplateView> GetTemplatesQuery();
    Task<IEnumerable<CompetitiveModeCertificate>> GetCompetitiveCertificates(string userId, CancellationToken cancellationToken);
    Task<IEnumerable<PracticeModeCertificate>> GetPracticeCertificates(string userId, CancellationToken cancellationToken);
}

internal class CertificatesService
(
    CoreOptions coreOptions,
    INowService now,
    IStore store,
    ITeamService teamsService
) : ICertificatesService
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly ITeamService _teamsService = teamsService;

    public async Task<string> BuildCertificateHtml(string templateId, CertificateHtmlContext htmlContext, CancellationToken cancellationToken)
    {
        var template = await _store
            .WithNoTracking<CertificateTemplate>()
            .SingleAsync(t => t.Id == templateId, cancellationToken);

        return BuildCertificateHtml(template.Content, htmlContext);
    }

    public async Task<string> BuildCertificateTemplatePreviewHtml(string templateId, CancellationToken cancellationToken)
    {
        var template = await _store
            .WithNoTracking<CertificateTemplate>()
            .SingleAsync(t => t.Id == templateId, cancellationToken);

        return BuildCertificateHtml(template.Content, null);
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

    public async Task<IEnumerable<CompetitiveModeCertificate>> GetCompetitiveCertificates(string userId, CancellationToken cancellationToken)
    {
        var now = _now.Get();

        var eligiblePlayerTeams = await _store
            .WithNoTracking<Data.Player>()
            .Where
            (
                p =>
                    p.UserId == userId
                    && (p.Game.GameEnd < now || p.Game.GameEnd == DateTimeOffset.MinValue)
                    && p.Game.CertificateTemplateId != null
            )
            .WhereDateIsNotEmpty(p => p.SessionEnd)
            .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .Select(p => new
            {
                p.Id,
                PlayerName = p.ApprovedName,
                p.TeamId,
                p.UserId,
                UserName = p.User.ApprovedName,
                p.Time,
                Game = new
                {
                    p.Game.Id,
                    p.Game.Name,
                    p.Game.GameEnd,
                    p.Game.Competition,
                    p.Game.Division,
                    p.Game.Season,
                    p.Game.Track,
                    p.Game.MinTeamSize,
                    MaxPossibleScore = p.Game.Challenges.Sum(c => c.Points),
                    PublishedCertificate = p.Game.PublishedCompetitiveCertificates.SingleOrDefault(c => c.OwnerUserId == userId)
                }
            })
            .OrderByDescending(p => p.Game.GameEnd)
            .ToArrayAsync(cancellationToken);

        // both should already be distinct, but let's not tempt fate, shall we?
        var teamIds = eligiblePlayerTeams.Select(p => p.TeamId).Distinct().ToArray();
        var gameIds = eligiblePlayerTeams.Select(p => p.Game.Id).Distinct().ToArray();

        var scoringData = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => teamIds.Contains(s.TeamId))
            .Where(s => gameIds.Contains(s.GameId))
            .ToDictionaryAsync(s => s.TeamId, s => s, cancellationToken);

        var gameParticipationCounts = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => gameIds.Contains(p.GameId))
            .GroupBy(p => p.GameId)
            .Select(gr => new
            {
                GameId = gr.Key,
                UniquePlayerCount = gr.Select(p => p.UserId).Distinct().Count(),
                UniqueTeamCount = gr.Select(p => p.TeamId).Distinct().Count()
            })
            .ToDictionaryAsync(thing => thing.GameId, thing => thing, cancellationToken);

        // #just553things
        var captains = await _teamsService.ResolveCaptains(teamIds, cancellationToken);

        return eligiblePlayerTeams.Select(t =>
        {
            captains.TryGetValue(t.TeamId, out var captain);
            scoringData.TryGetValue(t.TeamId, out var score);
            gameParticipationCounts.TryGetValue(t.Game.Id, out var participationCounts);

            return new CompetitiveModeCertificate
            {
                PlayerName = t.PlayerName,
                TeamName = captain is not null ? captain.ApprovedName : t.PlayerName,
                UserName = t.UserName,
                Game = new CertificateGameView
                {
                    Id = t.Game.Id,
                    Name = t.Game.Name,
                    Division = t.Game.Division,
                    Season = t.Game.Season,
                    Series = t.Game.Competition,
                    Track = t.Game.Track,
                    IsTeamGame = t.Game.MinTeamSize > 1,
                    MaxPossibleScore = t.Game.MaxPossibleScore
                },
                Date = t.Game.GameEnd.IsEmpty() ? captain.SessionEnd : t.Game.GameEnd,
                Rank = score?.Rank ?? 0,
                Score = score is not null ? score.ScoreOverall : 0,
                Duration = TimeSpan.FromMilliseconds(t.Time),
                UniquePlayerCount = participationCounts?.UniquePlayerCount,
                UniqueTeamCount = participationCounts?.UniqueTeamCount,
                PublishedOn = t.Game.PublishedCertificate?.PublishedOn
            };
        })
        .Where(c => c.Score > 0)
        .ToArray();
    }

    public async Task<IEnumerable<PracticeModeCertificate>> GetPracticeCertificates(string userId, CancellationToken cancellationToken)
    {
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Score >= c.Points)
            .Where(c => c.PlayerMode == PlayerMode.Practice)
            .Where(c => c.Player.UserId == userId)
            .WhereDateIsNotEmpty(c => c.LastScoreTime)
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList().OrderBy(c => c.StartTime).FirstOrDefault(), cancellationToken);

        // have to hit specs separately for now
        var specIds = challenges.Values.Select(c => c.SpecId);
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
                .Include(s => s.PublishedPracticeCertificates.Where(c => c.OwnerUserId == userId))
            .Where(s => specIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        // and teams 🙄 #just553things
        var teams = await _teamsService
            .ResolveCaptains(challenges.Select(c => c.Value).Select(c => c.TeamId).ToArray(), cancellationToken);

        return challenges
            .Select(entry => entry.Value)
            .Select(attempt =>
            {
                teams.TryGetValue(attempt.TeamId, out var captain);
                specs.TryGetValue(attempt.SpecId, out var spec);

                return new PracticeModeCertificate
                {
                    Challenge = new()
                    {
                        Id = attempt.Id,
                        Name = attempt.Name,
                        Description = spec is not null ? spec.Description : string.Empty,
                        ChallengeSpecId = attempt.SpecId
                    },
                    PlayerName = attempt.Player.User.ApprovedName,
                    UserName = attempt.Player.User.ApprovedName,
                    Date = attempt.StartTime,
                    Score = attempt.Score,
                    TeamName = captain is not null ? captain.ApprovedName : string.Empty,
                    Time = attempt.LastScoreTime - attempt.StartTime,
                    Game = new()
                    {
                        Id = attempt.GameId,
                        Name = attempt.Game.Name,
                        Division = attempt.Game.Division,
                        Season = attempt.Game.Season,
                        Series = attempt.Game.Competition,
                        Track = attempt.Game.Track,
                        IsTeamGame = attempt.Game.MinTeamSize > 1,
                        MaxPossibleScore = attempt.Points
                    },
                    PublishedOn = specs.TryGetValue(attempt.SpecId, out Data.ChallengeSpec value) ? value.PublishedPracticeCertificates.FirstOrDefault()?.PublishedOn : null
                };
            }).ToArray();
    }

    public IQueryable<CertificateTemplateView> GetTemplatesQuery()
        => _store
            .WithNoTracking<CertificateTemplate>()
            .Select(t => new CertificateTemplateView
            {
                Id = t.Id,
                Name = t.Name,
                Content = t.Content,
                CreatedByUser = new SimpleEntity { Id = t.CreatedByUserId, Name = t.CreatedByUser.ApprovedName },
                UseAsPracticeTemplateForGames = t.UseAsPracticeTemplateForGames.Select(g => new SimpleEntity { Id = g.Id, Name = g.Name }),
                UseAsTemplateForGames = t.UseAsTemplateForGames.Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            })
            .OrderBy(t => t.Name);

    private string BuildCertificateHtml(string templateHtml, CertificateHtmlContext htmlContext)
    {
        // load the outer template from this application (this is custom-crafted by us to ensure we end up
        // with a consistent HTML-compliant base)
        var outerTemplatePath = Path.Combine(_coreOptions.TemplatesDirectory, "certificate.template.html");
        var outerTemplate = File.ReadAllText(outerTemplatePath);
        var innerTemplate = templateHtml;

        // we allow the context to be null in this private function so that we can offer previews to admins
        if (htmlContext != null)
        {
            innerTemplate = templateHtml
                .Replace("{{challengeName}}", htmlContext.Challenge?.Name)
                .Replace("{{challengeDescription}}", htmlContext.Challenge?.Description)
                .Replace("{{gameName}}", htmlContext.Game?.Name)
                .Replace("{{date}}", htmlContext.Date.ToLocalTime().ToString("M/d/yyyy"))
                .Replace("{{division}}", htmlContext.Game?.Division)
                .Replace("{{playerName}}", htmlContext.UserRequestedName.IsNotEmpty() ? htmlContext.UserRequestedName : htmlContext.PlayerName)
                .Replace("{{rank}}", htmlContext.Rank?.ToString() ?? "--")
                .Replace("{{score}}", htmlContext.Score.ToString())
                .Replace("{{season}}", htmlContext.Game?.Season)
                .Replace("{{series}}", htmlContext.Game?.Series)
                .Replace("{{teamName}}", htmlContext.TeamName)
                .Replace("{{totalPlayerCount}}", htmlContext.TotalPlayerCount?.ToString() ?? "--")
                .Replace("{{totalTeamCount}}", htmlContext.TotalTeamCount?.ToString() ?? "--")
                .Replace("{{time}}", GetDurationDescription(htmlContext.Duration))
                .Replace("{{track}}", htmlContext.Game?.Track)
                .Replace("{{userName}}", htmlContext.UserName);
        }

        // compose final html and save to a temp file
        return outerTemplate.Replace("{{bodyContent}}", innerTemplate);
    }
}
