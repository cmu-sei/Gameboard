namespace Gameboard.Api.Structure;

public sealed class BackgroundTaskContext
{
    public string AccessToken { get; set; }
    public User ActingUser { get; set; }
    public string AppBaseUrl { get; set; }
}
