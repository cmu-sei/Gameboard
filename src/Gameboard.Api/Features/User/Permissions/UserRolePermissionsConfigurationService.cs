using System;
using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Users;

// we don't currently allow users to configure role permissions, so we just hard-code the map for now
// (interfaced and internal to allow injection to support unit testing)
internal interface IUserRolePermissionsConfigurationService
{
    IDictionary<UserRole, IEnumerable<PermissionKey>> GetConfiguration();
}

internal class UserRolePermissionsConfigurationService : IUserRolePermissionsConfigurationService
{
    public IDictionary<UserRole, IEnumerable<PermissionKey>> GetConfiguration()
    {
        return new Dictionary<UserRole, IEnumerable<PermissionKey>>()
        {
            {
                UserRole.Admin,
                Enum.GetValues<PermissionKey>()
            },
            {
                UserRole.Director,
                [
                    PermissionKey.Admin_View,
                    PermissionKey.Games_CreateEditDelete,
                    PermissionKey.Games_ViewUnpublished,
                    PermissionKey.Play_ChooseChallengeVariant,
                    PermissionKey.Play_IgnoreExecutionWindow,
                    PermissionKey.Play_IgnoreSessionResetSettings,
                    PermissionKey.Practice_EditSettings,
                    PermissionKey.Reports_View,
                    PermissionKey.Scores_AwardManualBonuses,
                    PermissionKey.Scores_RegradeAndRerank,
                    PermissionKey.Scores_ViewLive,
                    PermissionKey.Sponsors_CreateEdit,
                    PermissionKey.Support_ViewTickets,
                    PermissionKey.Teams_DeployGameResources,
                    PermissionKey.Teams_Observe
                ]
            },
            {
                UserRole.Support,
                [
                    PermissionKey.Admin_View,
                    PermissionKey.Games_ViewUnpublished,
                    PermissionKey.Play_ChooseChallengeVariant,
                    PermissionKey.Reports_View,
                    PermissionKey.Scores_ViewLive,
                    PermissionKey.Support_EditSettings,
                    PermissionKey.Support_ManageTickets,
                    PermissionKey.Support_ViewTickets,
                    PermissionKey.SystemNotifications_CreateEdit,
                    PermissionKey.Teams_ApproveNameChanges,
                    PermissionKey.Teams_Observe,
                    PermissionKey.Teams_Enroll,
                    PermissionKey.Teams_SetSyncStartReady,
                ]
            },
            {
                UserRole.Tester,
                [
                    PermissionKey.Admin_View,
                    PermissionKey.Games_ViewUnpublished,
                    PermissionKey.Play_ChooseChallengeVariant,
                    PermissionKey.Play_IgnoreExecutionWindow,
                    PermissionKey.Play_IgnoreSessionResetSettings,
                    PermissionKey.Teams_ApproveNameChanges,
                    PermissionKey.Teams_SetSyncStartReady,
                    PermissionKey.Teams_Enroll,
                ]
            },
            { UserRole.Member, Array.Empty<PermissionKey>() }
        };
    }
}
