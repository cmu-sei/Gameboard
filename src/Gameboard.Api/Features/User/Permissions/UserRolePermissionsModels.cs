using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Users;

public enum UserRolePermissionKey
{
    Admin_DeployGameResources,
    Admin_SendAnnouncements,
    Admin_View,
    EventHorizon_View,
    Games_AdminExternal,
    Games_ConfigureChallenges,
    Games_CreateEditDelete,
    Games_EnrollPlayers,
    Observe,
    Practice_EditSettings,
    Play_IgnoreSessionResetSettings,
    Play_IgnoreExecutionWindow,
    Players_ApproveNameChanges,
    Players_EditSession,
    Players_ViewActiveChallenges,
    Players_ViewSubmissions,
    Reports_View,
    Roles_EditRolePermissions,
    Scores_AwardManualBonuses,
    Scores_ViewLive,
    Sponsors_CreateEdit,
    Support_EditSettings,
    Support_ManageTickets,
    SystemNotifications_CreateEdit,
    Users_Create
}

public sealed class UserPermissionsCategory
{
    public required string Name { get; set; }
    public required IEnumerable<UserRolePermission> Permissions { get; set; }
}

public sealed class UserRolePermission
{
    public required bool IsRequiredForAdmin { get; set; }
    public required UserRolePermissionKey Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public sealed class UserRolePermissionException : GameboardValidationException
{
    public UserRolePermissionException(UserRole role, IEnumerable<UserRolePermissionKey> requiredPermissions)
        : base($"This operation requires the following permission(s): {string.Join(",", requiredPermissions)}. Your role ({role}) does not have one or more of these.") { }
}
