using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Data;
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

internal sealed class CantDemoteLastAdmin : GameboardValidationException
{
    public CantDemoteLastAdmin(string lastAdminId, UserRoleKey targetRole)
        : base($"Can't change the role of user {lastAdminId} to {targetRole}. Doing this would leave the system without administrators. Promote another user to {UserRoleKey.Admin} before changing this user's role.") { }
}
