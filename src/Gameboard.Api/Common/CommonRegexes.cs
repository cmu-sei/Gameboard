using System.Text.RegularExpressions;

namespace Gameboard.Api.Common;

public static partial class CommonRegexes
{
    public static readonly Regex WhitespaceGreedy = _WhitespaceGreedy();

    [GeneratedRegex(@"\s+", RegexOptions.Multiline)]
    private static partial Regex _WhitespaceGreedy();
}
