using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Common;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Users;

internal class IllegalApiKeyExpirationDate : GameboardException
{
    public IllegalApiKeyExpirationDate(DateTimeOffset date, DateTimeOffset now) : base($"Can't create an API key with expiration date ({date}) earlier than today ({now}).") { }
}

internal class ApiKeyNoName : GameboardException
{
    public ApiKeyNoName() : base($"API keys are required to have a value in their `Name` property.") { }
}

internal class CantCreateExistingUsers : GameboardValidationException
{
    public CantCreateExistingUsers(IEnumerable<string> userIds)
        : base($"Can't create {userIds.Count()} users: They already exist. (UserIds: {userIds.ToDelimited()})") { }
}
