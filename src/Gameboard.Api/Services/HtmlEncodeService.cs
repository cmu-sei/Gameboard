using System.Collections.Generic;
using System.Net;

namespace Gameboard.Api.Common;

public interface IHtmlEncodeService
{
    string Decode(string input);
    string Encode(string input);
}

internal class HtmlEncodeService : IHtmlEncodeService
{
    private static readonly IDictionary<string, string> PERMITTED_CHARACTER_REPLACEMENTS = new Dictionary<string, string>
    {
        { "&#39;", "'" }
    };

    public string Encode(string input)
    {
        if (input.IsEmpty())
            return string.Empty;

        var escaped = WebUtility.HtmlEncode(input);

        foreach (var replacement in PERMITTED_CHARACTER_REPLACEMENTS)
            escaped = escaped.Replace(replacement.Key, replacement.Value);

        return escaped;
    }

    public string Decode(string input)
    {
        if (input.IsEmpty())
            return string.Empty;

        foreach (var replacement in PERMITTED_CHARACTER_REPLACEMENTS)
            input = input.Replace(replacement.Value, replacement.Key);


        return WebUtility.HtmlDecode(input);
    }
}
