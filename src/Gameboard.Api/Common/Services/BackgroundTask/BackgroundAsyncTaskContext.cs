namespace Gameboard.Api.Common.Services;

public sealed class BackgroundAsyncTaskContext
{
    public User ActingUser { get; set; }
    public string AppBaseUrl { get; set; }
}
