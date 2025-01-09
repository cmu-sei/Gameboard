using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface IGameImportExportService
{
    GameImportExportBatch ExportGames(string[] gameIds, CancellationToken cancellationToken);
    ImportedGame[] ImportGames(GameImportExportBatch batch, CancellationToken cancellationToken);
}

internal sealed class GameImportExportService(IStore store) : IGameImportExportService
{
    private readonly IStore _store = store;

    public async GameImportExportBatch ExportGames(string[] gameIds, CancellationToken cancellationToken)
    {
        // pull the game data
        var finalGameIds = gameIds.Distinct().ToArray();
        var games = await _store
            .WithNoTracking<Data.Game>()
                .Include(g => g.CertificateTemplate)
                .Include(g => g.ExternalHost)
                .Include(g => g.PracticeCertificateTemplate)
                .Include(g => g.ChallengesFeedbackTemplate)
                .Include(g => g.FeedbackTemplate)
            .Where(g => finalGameIds.Contains(g.Id))
            .ToArrayAsync(cancellationToken);

        // all the attached entities are exported first, because we need to know their 
        // IDs to tie them to the games
        var certificateTemplates = games
            .Select(g => g.CertificateTemplate)
            .Union(games.Select(g => g.PracticeCertificateTemplate))
            .DistinctBy(t => t.Id)
            .ToArray();

        var externalHosts = games
            .Select(g => g.ExternalHost)
            .DistinctBy(h => h.Id)
            .ToArray();

        var feedbackTemplates = games
            .Select(g => g.ChallengesFeedbackTemplate)
            .Union(games.Select(g => g.FeedbackTemplate))
            .DistinctBy(t => t.Id)
            .ToArray();

        // export data for the related entities first
        // var exportCertificateTemplates = certificateTemplates
        //     .Select(t => new GameImportExportCertificateTemplate
        //     {
        //         ExportId
        //     })

        foreach (var game in games)
        {
            var exportGame = new GameImportExportGame
            {
                GameId = game.Id,
                Name = game.Name,
                Competition = game.Competition,
                Season = game.Season,
                Track = game.Track,
                Division = game.Division,
                AllowLateStart = game.AllowLateStart,
                AllowPreview = game.AllowPreview,
                AllowPublicScoreboardAccess = game.AllowPublicScoreboardAccess,
                AllowReset = game.AllowReset,
                CardText1 = game.CardText1,
                CardText2 = game.CardText2,
                CardText3 = game.CardText3,
                GameStart = game.GameStart,
                GameEnd = game.GameEnd,
                GameMarkdown = game.GameMarkdown,
                GamespaceLimitPerSession = game.GamespaceLimitPerSession,
                IsFeatured = game.IsFeatured,
                IsPublished = game.IsPublished,
                MaxAttempts = game.MaxAttempts,
                MaxTeamSize = game.MaxTeamSize,
                MinTeamSize = game.MinTeamSize,
                Mode = game.Mode,
                PlayerMode = game.PlayerMode,
                RegistrationClose = game.RegistrationClose,
                RegistrationOpen = game.RegistrationOpen,
                RegistrationMarkdown = game.RegistrationMarkdown,
                RegistrationType = game.RegistrationType,
                RequireSynchronizedStart = game.RequireSession,
                RequireSponsoredTeam = game.RequireSponsoredTeam,
                SessionAvailabilityWarningThreshold = game.SessionAvailabilityWarningThreshold,
                SessionLimit = game.SessionLimit,
                SessionMinutes = game.SessionMinutes,
                ShowOnHomePageInPracticeMode = game.ShowOnHomePageInPracticeMode
            };
        }

        // we need to know all of the related entities that accompany these games, because we want to create
        // and instance of each and tie them to the games through their exported ID
    }

    public ImportedGame[] ImportGames(GameImportExportBatch batch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
