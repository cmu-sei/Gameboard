namespace Gameboard.Api.Features.App;

public sealed class GetSettingsResponse
{
    public required PublicSettings Settings { get; set; }
}

public sealed class PublicSettings
{
    public bool NameChangeIsEnabled { get; set; }
}
