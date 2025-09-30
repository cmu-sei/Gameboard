// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.RegularExpressions;

namespace Gameboard.Api.Common;

public static partial class CommonRegexes
{
    public static readonly Regex WhitespaceGreedy = _WhitespaceGreedy();

    [GeneratedRegex(@"\s+", RegexOptions.Multiline)]
    private static partial Regex _WhitespaceGreedy();
}
