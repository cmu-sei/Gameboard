using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Users;

public enum PermissionKey
{
    Admin_CreateEditSponsors,
    Admin_View,
    Games_AdminExternal,
    Games_ConfigureChallenges,
    Games_CreateEditDelete,
    Play_IgnoreSessionResetSettings,
    Play_IgnoreExecutionWindow,
    Practice_EditSettings,
    Reports_View,
    Scores_AwardManualBonuses,
    Scores_ViewLive,
    Support_EditSettings,
    Support_ManageTickets,
    SystemNotifications_CreateEdit,
    Teams_ApproveNameChanges,
    Teams_DeployGameResources,
    Teams_EditSession,
    Teams_Enroll,
    Teams_Observe,
    Teams_SendAnnouncements,
    Users_Create
}

public enum PermissionKeyGroup
{
    Admin,
    Games,
    Play,
    Practice,
    Reports,
    Scoring,
    Support,
    Teams,
    Users
}

public sealed class UserRolePermissionCategory
{
    public required string Name { get; set; }
    public required IEnumerable<UserRolePermission> Permissions { get; set; }
}

public sealed class UserRolePermission
{
    public required PermissionKeyGroup Group { get; set; }
    public required PermissionKey Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public sealed class UserRolePermissionException : GameboardValidationException
{
    public UserRolePermissionException(UserRole role, IEnumerable<PermissionKey> requiredPermissions)
        : base($"This operation requires the following permission(s): {string.Join(",", requiredPermissions)}. Your role ({role}) does not have one or more of these.") { }
}
