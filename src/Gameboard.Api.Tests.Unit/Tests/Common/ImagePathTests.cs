// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Tests.Unit;

public class ImagePathTests
{
    [Theory]
    [InlineData("logo.png")]
    [InlineData("../logo.png")]
    [InlineData("../../appsettings.json")]
    [InlineData("../../../../../../etc/passwd")]
    [InlineData("subfolder/logo.png")]
    [InlineData("/etc/passwd")]
    public void Resolve_WithFileName_StaysInImageFolder(string fileName)
    {
        // given
        var imageFolder = Path.Combine(Path.DirectorySeparatorChar.ToString(), "app", "wwwroot", "img");

        // when
        var result = Path.GetFullPath(ImagePath.Resolve(imageFolder, fileName));

        // then
        Path.GetDirectoryName(result).ShouldBe(Path.GetFullPath(imageFolder));
    }

    [Fact]
    public void Resolve_WithPlainFileName_PreservesIt()
    {
        // given
        var imageFolder = Path.Combine(Path.DirectorySeparatorChar.ToString(), "app", "wwwroot", "img");

        // when
        var result = ImagePath.Resolve(imageFolder, "some-sponsor_abc123.png");

        // then
        result.ShouldBe(Path.Combine(imageFolder, "some-sponsor_abc123.png"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Resolve_WithNoFileName_ReturnsImageFolder(string? fileName)
    {
        // given
        var imageFolder = Path.Combine(Path.DirectorySeparatorChar.ToString(), "app", "wwwroot", "img");

        // when
        var result = ImagePath.Resolve(imageFolder, fileName!);

        // then
        result.ShouldBe(imageFolder);
    }
}
