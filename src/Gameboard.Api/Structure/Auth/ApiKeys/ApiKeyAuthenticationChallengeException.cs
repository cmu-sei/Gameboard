// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Structure.Auth;

public class ApiKeyAuthenticationChallengeException : Exception
{
    public ApiKeyAuthenticationChallengeException(string message = $"API key authentication does not support authentication challenges.") : base(message) { }
}

public class InvalidApiKeyAuthenticationOptions : Exception
{
    public InvalidApiKeyAuthenticationOptions(string message) : base(message) { }
}
