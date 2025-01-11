namespace Gameboard.Api.Features.Games;

public sealed class CantDownloadImage : GameboardException
{
    public CantDownloadImage(string gameId, string imageUrl) : base($"GameId {gameId}: Couldn't download image at {imageUrl}") { }
}

public sealed class ImageWasEmpty : GameboardException
{
    public ImageWasEmpty(string gameId, string imageUrl) : base($"GameID {gameId}: Image was empty ({imageUrl})") { }
}
