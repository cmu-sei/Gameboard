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
