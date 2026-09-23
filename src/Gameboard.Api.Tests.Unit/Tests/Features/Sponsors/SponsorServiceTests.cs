// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class SponsorServiceTests
{
    [Theory]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/svg+xml", ".svg")]
    [InlineData("image/webp", ".webp")]
    public void ResolveLogoFileExtension_WithPermittedMimeType_ReturnsExtension(string mimeType, string expected)
    {
        // given
        var sut = BuildSut("/app/wwwroot/img");

        // when
        var result = sut.ResolveLogoFileExtension(mimeType);

        // then
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/x-httpd-php")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveLogoFileExtension_WithProhibitedMimeType_Throws(string? mimeType)
    {
        // given
        var sut = BuildSut("/app/wwwroot/img");

        // when/then
        Should.Throw<ProhibitedMimeTypeUploaded>(() => sut.ResolveLogoFileExtension(mimeType!));
    }

    [Fact]
    public void ResolveLogoFileExtension_ForEveryAllowedMimeType_HasAnExtension()
    {
        // given
        var sut = BuildSut("/app/wwwroot/img");

        // when/then - the validator and the extension map must stay in sync
        foreach (var mimeType in sut.GetAllowedLogoMimeTypes())
        {
            sut.ResolveLogoFileExtension(mimeType).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("../../appsettings.json")]
    [InlineData("/etc/passwd")]
    [InlineData("subfolder/logo.png")]
    public void ResolveSponsorAvatarUri_WithPathInFileName_StaysInImageFolder(string storedLogo)
    {
        // given a sponsor whose stored logo tries to point outside the image folder
        var imageFolder = Path.Combine(Path.DirectorySeparatorChar.ToString(), "app", "wwwroot", "img");
        var sut = BuildSut(imageFolder);

        // when
        var result = Path.GetFullPath(sut.ResolveSponsorAvatarUri(storedLogo));

        // then
        Path.GetDirectoryName(result).ShouldBe(Path.GetFullPath(imageFolder));
    }

    private static SponsorService BuildSut(string imageFolder)
        => new
        (
            new CoreOptions { ImageFolder = imageFolder },
            new Defaults(),
            A.Fake<IMapper>(),
            A.Fake<IStore>(),
            A.Fake<IStore<Data.Sponsor>>()
        );
}
