// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO.Compression;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Tests.Integration;

public class GameExportImagePathTests : IClassFixture<GameboardTestContext>
{
    private const string CanaryFileName = "gb-export-canary.txt";
    private const string InsideImageFolder = "this file lives in the image folder";
    private const string OutsideImageFolder = "this file is outside the image folder and must not be exported";

    private readonly GameboardTestContext _testContext;

    public GameExportImagePathTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ExportGames_WithTraversalInSponsorLogo_ExportsTheFileInsideTheImageFolder
    (
        string gameId,
        string sponsorId,
        string actingUserId,
        IFixture fixture
    )
    {
        // given a sponsor whose stored logo is a traversal path, and two files with the same
        // name - one inside the image folder, one outside it
        await _testContext.WithDataState(state =>
        {
            // exporting logs a batch which is FK'd to the exporting user
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = actingUserId;
                u.Role = UserRoleKey.Admin;
            });

            state.Add<Data.Sponsor>(fixture, s =>
            {
                s.Id = sponsorId;
                s.Logo = $"../../{CanaryFileName}";
                s.ParentSponsorId = null;
                s.ParentSponsor = null;
            });

            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Sponsor = sponsorId;
                g.Logo = null;
                g.Background = null;
            });
        });

        var folders = CreateTempFolders();
        await File.WriteAllTextAsync(Path.Combine(folders.ImageFolder, CanaryFileName), InsideImageFolder);
        await File.WriteAllTextAsync(Path.Combine(folders.Root, CanaryFileName), OutsideImageFolder);

        var http = BuildExportClient(folders, actingUserId);

        // when the game is exported and the package downloaded
        var batch = await http
            .PostAsync("api/games/export", new ExportGamesCommand([gameId], false).ToJsonBody())
            .DeserializeResponseAs<GameImportExportBatch>();

        batch.ShouldNotBeNull();

        var packageResponse = await http.GetAsync($"api/games/export-batches/{batch.ExportBatchId}");
        packageResponse.IsSuccessStatusCode.ShouldBeTrue();

        var packageBytes = await packageResponse.Content.ReadAsByteArrayAsync();

        // then the packaged logo is the one from inside the image folder
        var logoContent = ReadPackagedSponsorLogo(packageBytes, sponsorId);

        logoContent.ShouldBe(InsideImageFolder);
        logoContent.ShouldNotBe(OutsideImageFolder);
    }

    private HttpClient BuildExportClient((string Root, string ImageFolder, string ExportFolder) folders, string actingUserId)
        => _testContext
            .BuildTestApplication
            (
                u =>
                {
                    u.Id = actingUserId;
                    u.Role = UserRoleKey.Admin;
                },
                configureTestServices: null,
                configureWebHost: builder => builder
                    .UseSetting("Core:ImageFolder", folders.ImageFolder)
                    .UseSetting("Core:ExportFolder", folders.ExportFolder)
            )
            .CreateClient();

    private static string ReadPackagedSponsorLogo(byte[] packageBytes, string sponsorId)
    {
        using var stream = new MemoryStream(packageBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entry = archive
            .Entries
            .SingleOrDefault(e => e.FullName.EndsWith($"sponsor-{sponsorId}-logo.txt"));

        entry.ShouldNotBeNull($"The export package should contain a logo for sponsor {sponsorId}. Entries: {string.Join(", ", archive.Entries.Select(e => e.FullName))}");

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);

        return reader.ReadToEnd();
    }

    private static (string Root, string ImageFolder, string ExportFolder) CreateTempFolders()
    {
        // the image folder is two levels deep so a traversal of "../.." lands in the root
        var root = Path.Combine(Path.GetTempPath(), $"gb-export-{Guid.NewGuid():n}");
        var imageFolder = Path.Combine(root, "wwwroot", "img");
        var exportFolder = Path.Combine(root, "export");

        Directory.CreateDirectory(imageFolder);
        Directory.CreateDirectory(exportFolder);

        return (root, imageFolder, exportFolder);
    }
}
