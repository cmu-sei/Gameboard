using System;

namespace Gameboard.Api.Features.Users;

internal class IllegalApiKeyExpirationDate : GameboardException
{
    public IllegalApiKeyExpirationDate(DateTimeOffset date, DateTimeOffset now) : base($"Can't create an API key with expiration date ({date}) earlier than today ({now}).") { }
}

internal class ApiKeyNoName : GameboardException
{
    public ApiKeyNoName() : base($"API keys are required to have a value in their `Name` property.") { }
}
