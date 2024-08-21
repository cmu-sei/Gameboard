using System.Collections.Generic;

namespace Gameboard.Api.Features.Users;

public sealed class UserRolePermissionsOverviewResponse
{
    public required UserRole YourRole { get; set; }
    public required IEnumerable<UserRolePermissionCategory> Categories { get; set; }
    public required IDictionary<UserRole, IEnumerable<PermissionKey>> RolePermissions { get; set; }
}
