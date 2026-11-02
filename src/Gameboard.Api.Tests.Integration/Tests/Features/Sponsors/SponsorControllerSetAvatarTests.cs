// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net.Http.Headers;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class SponsorControllerSetAvatarTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public SponsorControllerSetAvatarTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task SetAvatar_WithTraversalInFileName_DoesntEscapeImageFolder
    (
        string sponsorId,
        IFixture fixture
    )
    {
        // given a sponsor with no logo
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s =>
            {
                s.Id = sponsorId;
                s.Logo = null;
            });
        });

        var imageFolder = CreateTempImageFolder();

        // when an avatar is uploaded with a file name that tries to climb out of the image folder
        var response = await BuildAvatarClient(imageFolder)
            .PutAsync($"api/sponsor/{sponsorId}/avatar", BuildAvatarContent("../../gb-traversal-canary.png", MimeTypes.ImagePng));

        // then the upload lands inside the image folder and nowhere else
        response.IsSuccessStatusCode.ShouldBeTrue();

        Directory
            .GetFiles(imageFolder)
            .Select(Path.GetFileName)
            .ShouldHaveSingleItem();

        File.Exists(Path.Combine(imageFolder, "..", "..", "gb-traversal-canary.png")).ShouldBeFalse();

        // and the name we stored is a plain file name, not a path
        var logo = await GetStoredLogo(sponsorId);
        logo.ShouldBe(Path.GetFileName(logo));
        logo.ShouldStartWith(sponsorId.ToLower());
        logo.ShouldEndWith(".png");
    }

    [Theory, GbIntegrationAutoData]
    public async Task SetAvatar_WithMismatchedFileExtension_StoresExtensionForContentType
    (
        string sponsorId,
        IFixture fixture
    )
    {
        // given a sponsor with no logo
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s =>
            {
                s.Id = sponsorId;
                s.Logo = null;
            });
        });

        var imageFolder = CreateTempImageFolder();

        // when an avatar is uploaded as a permitted content type but named like a document
        var response = await BuildAvatarClient(imageFolder)
            .PutAsync($"api/sponsor/{sponsorId}/avatar", BuildAvatarContent("evil.html", MimeTypes.ImagePng));

        // then the stored file takes its extension from the content type we validated
        response.IsSuccessStatusCode.ShouldBeTrue();

        var logo = await GetStoredLogo(sponsorId);
        logo.ShouldEndWith(".png");
        logo.ShouldNotContain("evil");
    }

    [Theory, GbIntegrationAutoData]
    public async Task SetAvatar_WithSecondUpload_DeletesOnlyThePreviousLogo
    (
        string sponsorId,
        IFixture fixture
    )
    {
        // given a sponsor whose stored logo is a traversal path (as an older, vulnerable build could have written)
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s =>
            {
                s.Id = sponsorId;
                s.Logo = "../../gb-delete-canary.txt";
            });
        });

        var imageFolder = CreateTempImageFolder();
        var canaryPath = Path.GetFullPath(Path.Combine(imageFolder, "..", "..", "gb-delete-canary.txt"));
        await File.WriteAllTextAsync(canaryPath, "don't delete me");

        try
        {
            // when a new avatar is uploaded
            var response = await BuildAvatarClient(imageFolder)
                .PutAsync($"api/sponsor/{sponsorId}/avatar", BuildAvatarContent("logo.png", MimeTypes.ImagePng));

            // then cleaning up the "previous" logo doesn't reach outside the image folder
            response.IsSuccessStatusCode.ShouldBeTrue();
            File.Exists(canaryPath).ShouldBeTrue();
        }
        finally
        {
            File.Delete(canaryPath);
        }
    }

    private HttpClient BuildAvatarClient(string imageFolder)
        => _testContext
            .BuildTestApplication
            (
                u => u.Role = UserRoleKey.Admin,
                configureTestServices: null,
                configureWebHost: builder => builder.UseSetting("Core:ImageFolder", imageFolder)
            )
            .CreateClient();

    private static MultipartFormDataContent BuildAvatarContent(string fileName, string contentType)
    {
        var fileContent = new ByteArrayContent([0x1, 0x2, 0x3, 0x4]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // set the content disposition by hand so the file name reaches the API exactly as written
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"avatarFile\"",
            FileName = $"\"{fileName}\""
        };

        return new MultipartFormDataContent { fileContent };
    }

    private static string CreateTempImageFolder()
    {
        // two levels deep so a traversal of "../.." has somewhere to land
        var imageFolder = Path.Combine(Path.GetTempPath(), $"gb-avatar-{Guid.NewGuid():n}", "wwwroot", "img");
        Directory.CreateDirectory(imageFolder);

        return imageFolder;
    }

    private async Task<string> GetStoredLogo(string sponsorId)
    {
        var logo = default(string);

        await _testContext.ValidateStoreStateAsync(async db =>
        {
            logo = await db
                .Sponsors
                .Where(s => s.Id == sponsorId)
                .Select(s => s.Logo)
                .SingleAsync();
        });

        logo.ShouldNotBeNull();

        return logo;
    }
}
