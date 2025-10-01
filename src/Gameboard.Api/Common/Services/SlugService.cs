// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.RegularExpressions;

namespace Gameboard.Api.Common.Services;

public interface ISlugService
{
    public string Get(string input);
}

internal partial class SlugService : ISlugService
{
    public string Get(string input)
        => WhitespaceRegex()
            .Replace(input, match => "-")
            .ToLower()
            .Trim('-');

    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex WhitespaceRegex();
}
