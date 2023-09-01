namespace Gameboard.Api.Features.Games;

public static class GameExtensions
{
    public static bool IsTeamGame(this Game game)
        => IsTeamGame(game.MinTeamSize);

    public static bool IsTeamGame(this Data.Game game)
        => IsTeamGame(game.MinTeamSize);

    private static bool IsTeamGame(int minTeamSize)
        => minTeamSize > 1;
}
