// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Encodings.Web;

namespace Gameboard.Api.Common.Services;

public static class HtmlEncoder
{
    /// <summary>
    /// Safely encodes a string for inclusion in HTML content.
    ///
    /// Replaces HTML special characters with their entity equivalents,
    /// preventing XSS injection through unencoded template substitution.
    /// </summary>
    public static string Encode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return System.Net.WebUtility.HtmlEncode(value);
    }
}
