using System.Text.RegularExpressions;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeDocsService
{
    string ReplaceRelativeUris(string input);
}

internal partial class ChallengeDocsService : IChallengeDocsService
{
    [GeneratedRegex("\\[(.+)\\]\\((\\S+)\\)")]
    internal static partial Regex ChallengeDocLinksRegex();
    private readonly CoreOptions _coreOptions;

    public ChallengeDocsService(CoreOptions coreOptions) => _coreOptions = coreOptions;

    public string ReplaceRelativeUris(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var challengeDocUrl = _coreOptions.ChallengeDocUrl.TrimEnd('/');

        return ChallengeDocLinksRegex().Replace(input, m =>
        {
            if (m.Groups.Count == 3)
            {
                return @$"[{m.Groups[1]}]({challengeDocUrl}/{m.Groups[2]})";
            }

            return m.ToString();
        });
    }
}
