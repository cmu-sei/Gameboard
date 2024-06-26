using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class SessionWindowCalculatorTests
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
        var sut = new SessionWindowCalculator();

        // when the session length properties are calculated
        var result = sut.Calculate(game, false, sessionStart);

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
        var sut = new SessionWindowCalculator();

        // when the session length properties are calculated
        var result = sut.Calculate(game, false, sessionStart);

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
        var sut = new SessionWindowCalculator();

        // when the session length properties are calculated
        var result = sut.Calculate(game, false, sessionStart);

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
        var sut = new SessionWindowCalculator();

        // when the session length properties are calculated
        var result = sut.Calculate(game, false, sessionStart);

        // the session end should be equal to the game end
        result.IsLateStart.ShouldBeFalse();
    }

    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_OutsideExecutionAsAdmin_MarksNoLateStart(DateTimeOffset sessionStart)
    {
        // given a session for a game that would end in 2 hours
        var gameEnd = sessionStart.AddHours(2);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 180
        };

        var sut = new SessionWindowCalculator();

        // when an admin starts a session
        var result = sut.Calculate(game, true, sessionStart);

        // this should not be a late start
        result.IsLateStart.ShouldBeFalse();
        result.LengthInMinutes.ShouldBe(180);
    }

    [Theory, GameboardAutoData]
    public void CalculateSessionWindow_FullyOutsideExecutionAsAdmin_MarksNoLaterStart(DateTimeOffset sessionStart)
    {
        // given a a session for a game that ended an hour ago
        var gameEnd = sessionStart.AddHours(-1);

        var game = new Data.Game
        {
            GameEnd = gameEnd,
            SessionMinutes = 180
        };

        var sut = new SessionWindowCalculator();

        // when an admin starts a session
        var result = sut.Calculate(game, true, sessionStart);

        // this should also not be a late start
        result.IsLateStart.ShouldBeFalse();
        result.LengthInMinutes.ShouldBe(180);
    }
}
