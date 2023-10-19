namespace Gameboard.Api.Features.Games;

public static class GameExtensions
{
    public static bool IsTeamGame(this Game game)
        => IsTeamGame(game.MaxTeamSize);

    public static bool IsTeamGame(this Data.Game game)
        => IsTeamGame(game.MaxTeamSize);

    private static bool IsTeamGame(int maxTeamSize)
        => maxTeamSize > 1;

    public static int GetGamespaceLimit(this Data.Game game)
        => game.IsPracticeMode ? 1 : game.GamespaceLimitPerSession;
}
