using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Users;

public sealed class UserRolePermissionsOverviewResponse
{
    public required UserRoleKey? YourRole { get; set; }
    public required IEnumerable<UserRolePermissionCategory> Categories { get; set; }
    public required IDictionary<UserRoleKey, UserRole> Roles { get; set; }
}

public sealed class UserRoleSummary
{
    public required string Description { get; set; }
    public required IEnumerable<PermissionKey> Permissions { get; set; }
}
