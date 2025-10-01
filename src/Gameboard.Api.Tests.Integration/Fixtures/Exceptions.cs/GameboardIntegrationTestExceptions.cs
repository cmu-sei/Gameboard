// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class GameboardIntegrationTestException : Exception
{
    public GameboardIntegrationTestException(string message, Exception? innerException = null) : base(message, innerException) { }
}

internal class MissingServiceException<T> : GameboardIntegrationTestException where T : class
{
    public MissingServiceException(string? addlMessage)
        : base($"Couldn't resolve a required service of type {typeof(T).Name}.{(string.IsNullOrWhiteSpace(addlMessage) ? string.Empty : addlMessage)}") { }
}

internal class ResponseContentEmpty : GameboardIntegrationTestException
{
    public ResponseContentEmpty() : base("Deserialization failed because the body of the response is empty.") { }
}

internal class WrongExceptionType : GameboardIntegrationTestException
{
    public WrongExceptionType(Type expectedType, string responseContent)
        : base($"The response suggests that the wrong exception was thrown (expected {expectedType}). Message content: {responseContent}") { }
}
