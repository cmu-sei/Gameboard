namespace Gameboard.Api.Tests.Shared;

public abstract class GbAutomatedTestException : Exception
{
    public GbAutomatedTestException(string message) : base(message) { }
}

public class GbAutomatedTestSetupException : GbAutomatedTestException
{
    public GbAutomatedTestSetupException(string message) : base(message) { }
}
