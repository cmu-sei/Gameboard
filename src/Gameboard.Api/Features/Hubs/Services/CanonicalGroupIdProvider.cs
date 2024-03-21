namespace Gameboard.Api.Hubs;

public interface ICanonicalGroupIdProvider
{
    GameboardHubType GroupType { get; }
}

internal class CanonicalGroupIdProvider
{
    public string GetCanonicalGroupId(GameboardHubType groupType, string groupIdentifier)
        => $"{groupType.ToString().ToLower()}-{groupIdentifier}";
}
