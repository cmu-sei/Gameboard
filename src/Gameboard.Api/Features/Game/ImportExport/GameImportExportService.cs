using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Practice;
using Microsoft.EntityFrameworkCore;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;

namespace Gameboard.Api.Features.Games;

public interface IGameImportExportService
{
    Task<byte[]> GetExportedPackageContent(string exportBatchId, CancellationToken cancellationToken);
    Task<GameImportExportBatch> ExportPackage(string[] gameIds, bool includePracticeAreaTemplate, CancellationToken cancellationToken);
    Task<ImportedGame[]> ImportPackage(byte[] package, CancellationToken cancellationToken);
}

internal sealed class GameImportExportService
(
    IActingUserService actingUser,
    CoreOptions coreOptions,
    IGuidService guids,
    IJsonService json,
    IPracticeService practice,
    IStore store
) : IGameImportExportService
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IGuidService _guids = guids;
    private readonly IJsonService _json = json;
    private readonly IPracticeService _practice = practice;
    private readonly IStore _store = store;

    public async Task<byte[]> GetExportedPackageContent(string exportBatchId, CancellationToken cancellationToken)
    {
        var path = GetExportBatchPackagePath(exportBatchId);
        if (!File.Exists(path))
        {
            throw new ExportPackageNotFound(exportBatchId);
        }

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async Task<GameImportExportBatch> ExportPackage(string[] gameIds, bool includePracticeAreaTemplate, CancellationToken cancellationToken)
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
                .Include(g => g.ChallengesFeedbackTemplate)
                .Include(g => g.FeedbackTemplate)
                .Include(g => g.ExternalHost)
                .Include(g => g.PracticeCertificateTemplate)
                .Include(g => g.Specs)
            .Where(g => finalGameIds.Contains(g.Id))
            .AsSplitQuery()
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
                RequireSynchronizedStart = game.RequireSynchronizedStart,
                RequireSponsoredTeam = game.RequireSponsoredTeam,
                SessionAvailabilityWarningThreshold = game.SessionAvailabilityWarningThreshold,
                SessionLimit = game.SessionLimit,
                SessionMinutes = game.SessionMinutes,
                ShowOnHomePageInPracticeMode = game.ShowOnHomePageInPracticeMode,

                // specs
                Specs = game.Specs.Select(s => new GameImportExportChallengeSpec
                {
                    Description = s.Description,
                    Disabled = s.Disabled,
                    ExternalId = s.ExternalId,
                    GameEngineType = s.GameEngineType,
                    IsHidden = s.IsHidden,
                    Name = s.Name,
                    Points = s.Points,
                    ShowSolutionGuideInCompetitiveMode = s.ShowSolutionGuideInCompetitiveMode,
                    Tag = s.Tag,
                    Tags = s.Tags,
                    Text = s.Text,
                    X = s.X,
                    Y = s.Y,
                    R = s.R
                })
                .ToArray(),

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
        using (var stream = File.OpenWrite(Path.Combine(GetExportBatchRootPath(exportBatchId), "manifest.json")))
        {
            await _json.SerializeAsync(batch, stream);
        }

        // zip zip zip
        using (var archive = ZipArchive.Create())
        {
            archive.AddAllFromDirectory(GetExportBatchRootPath(exportBatchId));
            archive.SaveTo(GetExportBatchPackagePath(exportBatchId), new WriterOptions(CompressionType.Deflate));
        }

        return batch;
    }

    public async Task<ImportedGame[]> ImportPackage(byte[] package, CancellationToken cancellationToken)
    {
        var importBatchId = _guids.Generate();
        var actingUser = _actingUser.Get();
        Directory.CreateDirectory(GetImportBatchRoot(importBatchId));
        Directory.CreateDirectory(GetImportPackageRoot());
        Directory.CreateDirectory(GetImportBatchImageRoot(importBatchId));

        // extract the data
        var tempArchivePath = Path.Combine(GetImportPackageRoot(), importBatchId) + ".zip";
        using (var tempFile = File.Open(tempArchivePath, FileMode.Create))
        {
            await tempFile.WriteAsync(package, cancellationToken);
        }

        using (var tempArchiveStream = File.OpenRead(tempArchivePath))
        {
            using (var reader = ReaderFactory.Open(tempArchiveStream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(GetImportBatchRoot(importBatchId), new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        // copy image files
        foreach (var imgPath in Directory.EnumerateFileSystemEntries(GetImportBatchImageRoot(importBatchId)))
        {
            var fileName = Path.GetFileName(imgPath);
            File.Copy(imgPath, Path.Combine(_coreOptions.ImageFolder, fileName), true);
        }

        // now read the manifest
        var importBatch = default(GameImportExportBatch);
        using (var manifestStream = File.OpenRead(Path.Combine(GetImportBatchRoot(importBatchId), "manifest.json")))
        {
            importBatch = await _json.DeserializeAsync<GameImportExportBatch>(manifestStream);
        }

        // certificate templates
        if (importBatch.CertificateTemplates.Any())
        {
            var certificateTemplates = importBatch
                .CertificateTemplates
                .Values
                .Select(t => new CertificateTemplate
                {
                    Content = t.Content,
                    CreatedByUserId = actingUser.Id,
                    Name = t.Name
                })
                .ToArray();

            await _store.SaveAddRange(certificateTemplates);

            if (importBatch.PracticeAreaCertificateTemplateId.IsNotEmpty())
            {
                var updatedSettings = await _practice.GetSettings(cancellationToken);
                updatedSettings.CertificateTemplateId = importBatch.PracticeAreaCertificateTemplateId;
                await _practice.UpdateSettings(updatedSettings, actingUser.Id, cancellationToken);
            }
        }

        // external hosts
        if (importBatch.ExternalHosts.Any())
        {
            await _store.SaveAddRange
            (
                importBatch
                    .ExternalHosts
                    .Values
                    .Select(h => new ExternalGameHost
                    {
                        ClientUrl = h.ClientUrl,
                        DestroyResourcesOnDeployFailure = h.DestroyResourcesOnDeployFailure,
                        GamespaceDeployBatchSize = h.GamespaceDeployBatchSize,
                        HostUrl = h.HostUrl,
                        HttpTimeoutInSeconds = h.HttpTimeoutInSeconds,
                        Name = h.Name,
                        PingEndpoint = h.PingEndpoint,
                        StartupEndpoint = h.StartupEndpoint,
                        TeamExtendedEndpoint = h.TeamExtendedEndpoint
                    })
                    .ToArray()
            );
        }

        // feedback templates
        var feedbackTemplates = importBatch
            .FeedbackTemplates
            .Values
            .Select(t => new FeedbackTemplate
            {
                Content = t.Content,
                CreatedByUserId = actingUser.Id,
                HelpText = t.HelpText,
                Name = t.Name
            })
            .ToArray();
        await _store.SaveAddRange(feedbackTemplates);

        // sponsors
        if (importBatch.Sponsors.Any())
        {
            await _store.SaveAddRange
            (
                importBatch
                    .Sponsors
                    .Values
                    .Select(s => new Data.Sponsor
                    {
                        Approved = s.Approved,
                        Logo = s.LogoFileName,
                        Name = s.Name,
                        ParentSponsor = s.ParentSponsor is null ? null : new Data.Sponsor
                        {
                            Approved = s.ParentSponsor.Approved,
                            Logo = s.ParentSponsor.LogoFileName,
                            Name = s.ParentSponsor.Id,
                        }
                    })
                    .ToArray()
            );
        }

        // and now games!
        var importedGames = importBatch.Games.Select(g => new Data.Game
        {
            Name = g.Name,
            Competition = g.Competition,
            Season = g.Season,
            Track = g.Track,
            Division = g.Division,
            AllowLateStart = g.AllowLateStart,
            AllowPreview = g.AllowPreview,
            AllowPublicScoreboardAccess = g.AllowPublicScoreboardAccess,
            AllowReset = g.AllowReset,
            Background = g.MapImageFileName,
            CardText1 = g.CardText1,
            CardText2 = g.CardText2,
            CardText3 = g.CardText3,
            GameStart = g.GameStart ?? DateTime.MinValue,
            GameEnd = g.GameEnd ?? DateTime.MinValue,
            GameMarkdown = g.GameMarkdown,
            GamespaceLimitPerSession = g.GamespaceLimitPerSession,
            IsFeatured = g.IsFeatured,
            IsPublished = g.IsPublished,
            Logo = g.CardImageFileName,
            MaxAttempts = g.MaxAttempts ?? 0,
            MaxTeamSize = g.MaxTeamSize,
            MinTeamSize = g.MinTeamSize,
            Mode = g.Mode,
            PlayerMode = g.PlayerMode,
            RegistrationClose = g.RegistrationClose ?? DateTime.MinValue,
            RegistrationOpen = g.RegistrationOpen ?? DateTime.MinValue,
            RegistrationMarkdown = g.RegistrationMarkdown,
            RegistrationType = g.RegistrationType,
            RequireSynchronizedStart = g.RequireSynchronizedStart,
            RequireSponsoredTeam = g.RequireSponsoredTeam,
            SessionAvailabilityWarningThreshold = g.SessionAvailabilityWarningThreshold,
            SessionLimit = g.SessionLimit ?? 0,
            SessionMinutes = g.SessionMinutes,
            ShowOnHomePageInPracticeMode = g.ShowOnHomePageInPracticeMode,

            // specs
            Specs = g.Specs.Select(s => new Data.ChallengeSpec
            {
                Id = _guids.Generate(),
                Description = s.Description,
                Disabled = s.Disabled,
                ExternalId = s.ExternalId,
                GameEngineType = s.GameEngineType,
                IsHidden = s.IsHidden,
                Name = s.Name,
                Points = s.Points,
                ShowSolutionGuideInCompetitiveMode = s.ShowSolutionGuideInCompetitiveMode,
                Tag = s.Tag,
                Tags = s.Tags,
                Text = s.Text,
                X = s.X,
                Y = s.Y,
                R = s.R
            })
            .ToArray(),

            // related entities
            CertificateTemplateId = g.CertificateTemplateId,
            ChallengesFeedbackTemplateId = g.ChallengesFeedbackTemplateId,
            ExternalHostId = g.ExternalHostId,
            FeedbackTemplateId = g.FeedbackTemplateId,
            PracticeCertificateTemplateId = g.PracticeCertificateTemplateId,
            Sponsor = g.SponsorId
        })
        .ToArray();

        await _store.SaveAddRange(importedGames);

        return importedGames.Select(g => new ImportedGame
        {
            Id = g.Id,
            Name = g.Name
        })
        .ToArray();
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
        => Path.Combine(GetExportBatchRootPath(exportBatchId), "images");

    private string GetImportPackageRoot()
        => Path.Combine(_coreOptions.ImportFolder, "packages");

    private string GetImportBatchRoot(string importBatchId)
        => Path.Combine(_coreOptions.ImportFolder, "temp", importBatchId);

    private string GetImportBatchImageRoot(string importBatchId)
        => Path.Combine(_coreOptions.ImportFolder, "temp", importBatchId, "images");

    private string GetCardImageFileName(string gameId, string extension)
        => $"game-{gameId}-card{extension}";

    private string GetMapImageFileName(string gameId, string extension)
        => $"game-{gameId}-map{extension}";

    private string GetSponsorLogoFileName(string sponsorId, string extension)
        => $"sponsor-{sponsorId}-logo{extension}";
}
