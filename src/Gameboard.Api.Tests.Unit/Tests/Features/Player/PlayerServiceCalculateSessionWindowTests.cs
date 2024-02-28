namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceCalculateSessionWindowTests
{
    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_WithLateStart_ShortensSession(DateTimeOffset sessionStart)
    {
        // given a session starting now for a game with a longer session time
        // than is remaining in the execution period
        var gameEnd = sessionStart.AddHours(1);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 120
        };
        var sut = PlayerServiceTestHelpers.GetTestableSut();

        // when the session length properties are calculated
        var result = sut.CalculateSessionWindow(game, false, sessionStart);

        // then session end should be equal to the game end
        result.End.ShouldBe(gameEnd);
    }

    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_WithNoLateStart_PreservesSessionLength(DateTimeOffset sessionStart)
    {
        // given a session starting now for a game with a longer session time
        // than is remaining in the execution period
        var gameEnd = sessionStart.AddHours(2);
        var sessionEnd = sessionStart.AddHours(1);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 60
        };
        var sut = PlayerServiceTestHelpers.GetTestableSut();

        // when the session length properties are calculated
        var result = sut.CalculateSessionWindow(game, false, sessionStart);

        // then session end should be equal to the game end
        result.End.ShouldBe(sessionEnd);
    }

    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_WithLateStart_MarksLateStart(DateTimeOffset sessionStart)
    {
        // given a session starting now for a game with a longer session time
        // than is remaining in the execution period
        var gameEnd = sessionStart.AddHours(1);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 120
        };
        var sut = PlayerServiceTestHelpers.GetTestableSut();

        // when the session length properties are calculated
        var result = sut.CalculateSessionWindow(game, false, sessionStart);

        // the session end should be equal to the game end
        result.IsLateStart.ShouldBeTrue();
    }

    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_WithNoLateStart_MarksNoLateStart(DateTimeOffset sessionStart)
    {
        // given a session starting now for a game with a longer session time
        // than is remaining in the execution period
        var gameEnd = sessionStart.AddHours(2);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 60
        };
        var sut = PlayerServiceTestHelpers.GetTestableSut();

        // when the session length properties are calculated
        var result = sut.CalculateSessionWindow(game, false, sessionStart);

        // the session end should be equal to the game end
        result.IsLateStart.ShouldBeFalse();
    }
}
