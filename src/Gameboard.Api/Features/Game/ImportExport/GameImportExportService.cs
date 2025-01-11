using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Games;

public interface IGameImportExportService
{
    Task<GameImportExportBatch> ExportGames(string[] gameIds, bool includePracticeAreaTemplate, CancellationToken cancellationToken);
    Task<ImportedGame[]> ImportGames(GameImportExportBatch batch, CancellationToken cancellationToken);
}

internal sealed class GameImportExportService
(
    CoreOptions coreOptions,
    IGuidService guids,
    HttpClient http,
    IJsonService json,
    IStore store,
    IZipService zip
) : IGameImportExportService
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IGuidService _guids = guids;
    private readonly HttpClient _http = http;
    private readonly IJsonService _json = json;
    private readonly IStore _store = store;
    private readonly IZipService _zip = zip;

    public async Task<GameImportExportBatch> ExportGames(string[] gameIds, bool includePracticeAreaTemplate, CancellationToken cancellationToken)
    {
        // declare a batch number - we'll use this to identify this attempt at exporting
        var exportBatchId = _guids.Generate();

        // create the output directories for the package
        Directory.CreateDirectory(GetExportBatchRootPath(exportBatchId));
        Directory.CreateDirectory(GetExportBatchImgRootPath(exportBatchId));
        Directory.CreateDirectory(GetExportPackageRoot());

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
        // IDs to tie them to the games. For each, we put into a dict with the key as the
        // auto-generated exportedId
        var certificateTemplates = games
            .Select(g => g.CertificateTemplate)
            .Union(games.Select(g => g.PracticeCertificateTemplate))
            .Where(t => t != null)
            .DistinctBy(t => t.Id)
            .ToDictionary(t => t.Id, t => new GameImportExportCertificateTemplate
            {
                Id = t.Id,
                Name = t.Name,
                Content = t.Content
            });

        // we also include the practice area default template if requested
        var exportPracticeAreaTemplate = default(GameImportExportCertificateTemplate);
        if (includePracticeAreaTemplate)
        {
            exportPracticeAreaTemplate = await _store
                .WithNoTracking<CertificateTemplate>()
                .Where(t => t.UsedAsPracticeModeDefault != null)
                .OrderBy(t => t.Name)
                .Select(t => new GameImportExportCertificateTemplate
                {
                    Id = t.Id,
                    Name = t.Name,
                    Content = t.Content
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (exportPracticeAreaTemplate is not null && !certificateTemplates.ContainsKey(exportPracticeAreaTemplate.Id))
            {
                certificateTemplates.Add(exportPracticeAreaTemplate.Id, exportPracticeAreaTemplate);
            }
        }

        var externalHosts = games
            .Select(g => g.ExternalHost)
            .Where(h => h != null)
            .DistinctBy(h => h.Id)
            .ToDictionary(h => h.Id, h => new GameImportExportExternalHost
            {
                Id = h.Id,
                Name = h.Name,
                ClientUrl = h.ClientUrl,
                DestroyResourcesOnDeployFailure = h.DestroyResourcesOnDeployFailure,
                GamespaceDeployBatchSize = h.GamespaceDeployBatchSize,
                HttpTimeoutInSeconds = h.HttpTimeoutInSeconds,
                // we omit API key for security
                HostUrl = h.HostUrl,
                PingEndpoint = h.PingEndpoint,
                StartupEndpoint = h.StartupEndpoint,
                TeamExtendedEndpoint = h.TeamExtendedEndpoint
            });

        var feedbackTemplates = games
            .Select(g => g.ChallengesFeedbackTemplate)
            .Union(games.Select(g => g.FeedbackTemplate))
            .Where(t => t != null)
            .DistinctBy(t => t.Id)
            .ToDictionary(t => t.Id, t => new GameImportExportFeedbackTemplate
            {
                Id = t.Id,
                Name = t.Name,
                Content = t.Content,
                HelpText = t.HelpText
            });

        // build exported entities for the related stuff
        var exportedCertificates = certificateTemplates.Values.Select(t => new GameImportExportCertificateTemplate
        {
            Id = t.Id,
            Name = t.Name,
            Content = t.Content
        });

        // sponsor is currently not a proper FK, so we have to manually retrieve them (and any parent sponsors)
        var sponsorIds = games
            .Select(g => g.Sponsor)
            .Where(s => s.IsNotEmpty())
            .ToArray();
        var sponsors = await _store
            .WithNoTracking<Data.Sponsor>()
                .Include(s => s.ParentSponsor)
            .Where(s => sponsorIds.Contains(s.Id))
            .ToArrayAsync(cancellationToken);

        var exportedSponsors = new Dictionary<string, GameImportExportSponsor>();
        foreach (var sponsor in sponsors)
        {
            var logoFileName = default(string);
            if (sponsor.Logo.IsNotEmpty())
            {
                var logoFilePath = Path.Combine(_coreOptions.ImageFolder, sponsor.Logo);
                var destinationPath = Path.Combine
                (
                    GetExportBatchImgRootPath(exportBatchId),
                    GetSponsorLogoFileName(sponsor.Id, Path.GetExtension(logoFilePath))
                );

                File.Copy(logoFilePath, destinationPath);
                logoFileName = Path.GetFileName(destinationPath);
            }

            var parentLogoFileName = default(string);
            if (sponsor.ParentSponsorId.IsNotEmpty())
            {
                var logoFilePath = Path.Combine(_coreOptions.ImageFolder, sponsor.ParentSponsor.Logo);
                var destinationPath = Path.Combine
                (
                    GetExportBatchImgRootPath(exportBatchId),
                    GetSponsorLogoFileName(sponsor.ParentSponsorId, Path.GetExtension(logoFilePath))
                );

                File.Copy(logoFilePath, destinationPath);
                parentLogoFileName = Path.GetFileName(destinationPath);
            }

            exportedSponsors.Add(sponsor.Id, new GameImportExportSponsor
            {
                Id = sponsor.Id,
                Name = sponsor.Name,
                LogoFileName = logoFileName,
                Approved = sponsor.Approved,
                ParentSponsor = sponsor.ParentSponsorId == null ? null : new GameImportExportSponsor
                {
                    Id = sponsor.ParentSponsorId,
                    Name = sponsor.ParentSponsor.Name,
                    LogoFileName = parentLogoFileName,
                    Approved = sponsor.ParentSponsor.Approved,
                    ParentSponsor = null
                }
            });
        }

        var gamesExported = new List<GameImportExportGame>();
        foreach (var game in games)
        {
            var gameCardImageFileName = default(string);
            var gameMapImageFileName = default(string);

            if (game.Logo.IsNotEmpty())
            {
                var fileName = Path.Combine(_coreOptions.ImageFolder, game.Logo);
                var extension = Path.GetExtension(fileName);
                var destinationFileName = Path.Combine
                (
                    GetExportBatchImgRootPath(exportBatchId),
                    GetCardImageFileName(game.Id, extension)
                );
                File.Copy(fileName, destinationFileName);

                gameCardImageFileName = Path.GetFileName(destinationFileName);
            }

            if (game.Background.IsNotEmpty())
            {
                var fileName = Path.Combine(_coreOptions.ImageFolder, game.Background);
                var extension = Path.GetExtension(fileName);
                var destinationFileName = Path.Combine
                (
                    GetExportBatchImgRootPath(exportBatchId),
                    GetMapImageFileName(game.Id, extension)
                );

                File.Copy(fileName, destinationFileName);

                gameMapImageFileName = Path.GetFileName(destinationFileName);
            }

            gamesExported.Add(new GameImportExportGame
            {
                Id = game.Id,
                Name = game.Name,
                Competition = game.Competition,
                Season = game.Season,
                Track = game.Track,
                Division = game.Division,
                AllowLateStart = game.AllowLateStart,
                AllowPreview = game.AllowPreview,
                AllowPublicScoreboardAccess = game.AllowPublicScoreboardAccess,
                AllowReset = game.AllowReset,
                CardImageFileName = gameCardImageFileName,
                CardText1 = game.CardText1,
                CardText2 = game.CardText2,
                CardText3 = game.CardText3,
                GameStart = game.GameStart,
                GameEnd = game.GameEnd,
                GameMarkdown = game.GameMarkdown,
                GamespaceLimitPerSession = game.GamespaceLimitPerSession,
                IsFeatured = game.IsFeatured,
                IsPublished = game.IsPublished,
                MapImageFileName = gameMapImageFileName,
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
                ShowOnHomePageInPracticeMode = game.ShowOnHomePageInPracticeMode,

                // related entities
                CertificateTemplateId = game.CertificateTemplateId,
                ChallengesFeedbackTemplateId = game.ChallengesFeedbackTemplateId,
                ExternalHostId = game.ExternalHostId,
                FeedbackTemplateId = game.FeedbackTemplateId,
                PracticeCertificateTemplateId = game.PracticeCertificateTemplateId,
                SponsorId = game.Sponsor
            });
        }

        var batch = new GameImportExportBatch
        {
            DownloadUrl = GetExportBatchPackageName(exportBatchId),
            ExportBatchId = exportBatchId,
            Games = [.. gamesExported],
            CertificateTemplates = certificateTemplates,
            ExternalHosts = externalHosts,
            FeedbackTemplates = feedbackTemplates,
            PracticeAreaCertificateTemplateId = exportPracticeAreaTemplate?.Id,
            Sponsors = exportedSponsors
        };

        // write the manifest
        using var stream = File.OpenWrite(Path.Combine(GetExportBatchRootPath(exportBatchId), "manifest.json"));
        await _json.SerializeAsync(batch, stream);

        // zip zip zip
        _zip.ZipDirectory
        (
            GetExportBatchPackagePath(exportBatchId),
            GetExportBatchRootPath(exportBatchId)
        );

        return batch;
    }

    public Task<ImportedGame[]> ImportGames(GameImportExportBatch batch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<string> DownloadImage(string gameId, string url, string localFileName, CancellationToken cancellationToken)
    {
        var imageBytes = default(byte[]);
        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch
        {
            throw new CantDownloadImage(gameId, url);
        }

        if (imageBytes.Length > 0)
        {
            await File.WriteAllBytesAsync(localFileName, imageBytes, cancellationToken);
        }
        else
        {
            throw new ImageWasEmpty(gameId, url);
        }

        return localFileName;
    }

    private string GetExportBatchPackageName(string exportBatchId)
        => $"{_coreOptions.AppName.ToLower()}-games-{exportBatchId}";

    private string GetExportBatchPackagePath(string exportBatchId)
        => $"{Path.Combine(GetExportPackageRoot(), GetExportBatchPackageName(exportBatchId) + ".zip")}";

    private string GetExportPackageRoot()
        => Path.Combine(_coreOptions.ExportFolder, "packages");

    private string GetExportBatchRootPath(string exportBatchId)
        => Path.Combine(_coreOptions.ExportFolder, "temp", exportBatchId);

    private string GetExportBatchImgRootPath(string exportBatchId)
        => Path.Combine(GetExportBatchRootPath(exportBatchId), "img");

    private string GetCardImageFileName(string gameId, string extension)
        => $"game-{gameId}-card{extension}";

    private string GetMapImageFileName(string gameId, string extension)
        => $"game-{gameId}-map{extension}";

    private string GetSponsorLogoFileName(string sponsorId, string extension)
        => $"sponsor-{sponsorId}-logo{extension}";
}
