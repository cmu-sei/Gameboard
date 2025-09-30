// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;

namespace Gameboard.Api.Features.ApiKeys;

internal class InvalidApiKey : GameboardException
{
    public InvalidApiKey(string headerValue) : base($"""Your API key is invalid. (You sent: "{headerValue}")""") { }
}

internal class InvalidApiKeyFormat : GameboardException
{
    public InvalidApiKeyFormat(string headerValue) : base($"""Your API key is formatted incorrectly. Verify that you're sending the correct value or ask an admin to generate a new API key for this user account. (You sent: "{headerValue}")""") { }
}
