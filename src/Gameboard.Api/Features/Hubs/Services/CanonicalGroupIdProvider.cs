namespace Gameboard.Api.Hubs;

public interface ICanonicalGroupIdProvider
{
    GameboardHubGroupType GroupType { get; }
}

internal class CanonicalGroupIdProvider
{
    public string GetCanonicalGroupId(GameboardHubGroupType groupType, string groupIdentifier)
        => $"{groupType.ToString().ToLower()}-{groupIdentifier}";
}
