// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Common.Services;

public static class HtmlEncoder
{
    /// <summary>
    /// Safely encodes a string for inclusion in HTML content.
    ///
    /// Replaces HTML special characters with their entity equivalents,
    /// preventing XSS injection through unencoded template substitution.
    /// </summary>
    /// <param name="value">The value to encode. May be null, in which case an empty string is returned.</param>
    public static string Encode(string value)
        => System.Net.WebUtility.HtmlEncode(value) ?? string.Empty;
}
