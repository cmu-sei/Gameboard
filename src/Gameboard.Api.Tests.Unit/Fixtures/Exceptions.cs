namespace Gameboard.Api.Tests.Unit.Fixtures;

public class GameboardApiTestsFakingException : Exception
{
    public GameboardApiTestsFakingException(string message) : base(message) { }
}
