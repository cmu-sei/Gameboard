using System;

namespace Gameboard.Api.Common;

public class GameboardException : Exception
{
    internal GameboardException(string message) : base(message) { }
    internal GameboardException(string message, Exception innerException) : base(message, innerException) { }
}

internal class SemaphoreLockFailure : GameboardException
{
    public SemaphoreLockFailure(Exception ex) : base($"An operation inside a semaphore lock failed.", ex) { }
}
