// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Users;

// we don't currently allow users to configure role permissions, so we just hard-code the map for now
// (interfaced and internal to allow injection to support unit testing)
internal interface IUserRolePermissionsConfigurationService
{
    IDictionary<UserRoleKey, IEnumerable<PermissionKey>> GetConfiguration();
}

internal class UserRolePermissionsConfigurationService : IUserRolePermissionsConfigurationService
{
    public IDictionary<UserRoleKey, IEnumerable<PermissionKey>> GetConfiguration()
    {
        return new Dictionary<UserRoleKey, IEnumerable<PermissionKey>>()
        {
            {
                UserRoleKey.Admin,
                Enum.GetValues<PermissionKey>()
            },
            {
                UserRoleKey.Director,
                [
                    PermissionKey.Admin_View,
                    PermissionKey.Games_CreateEditDelete,
                    PermissionKey.Games_DeleteWithPlayerData,
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
                    PermissionKey.Support_EditSettings,
                    PermissionKey.Support_ManageTickets,
                    PermissionKey.Support_ViewTickets,
                    PermissionKey.SystemNotifications_CreateEdit,
                    PermissionKey.Teams_ApproveNameChanges,
                    PermissionKey.Teams_DeployGameResources,
                    PermissionKey.Teams_EditSession,
                    PermissionKey.Teams_Enroll,
                    PermissionKey.Teams_Observe,
                    PermissionKey.Teams_SendAnnouncements,
                    PermissionKey.Teams_SetSyncStartReady,
                ]
            },
            {
                UserRoleKey.Support,
                [
                    PermissionKey.Admin_View,
                    PermissionKey.Games_ViewUnpublished,
                    PermissionKey.Play_ChooseChallengeVariant,
                    PermissionKey.Play_IgnoreExecutionWindow,
                    PermissionKey.Play_IgnoreSessionResetSettings,
                    PermissionKey.Reports_View,
                    PermissionKey.Scores_ViewLive,
                    PermissionKey.Support_EditSettings,
                    PermissionKey.Support_ManageTickets,
                    PermissionKey.Support_ViewTickets,
                    PermissionKey.Teams_ApproveNameChanges,
                    PermissionKey.Teams_Observe,
                    PermissionKey.Teams_Enroll,
                ]
            },
            {
                UserRoleKey.Tester,
                [
                    PermissionKey.Games_ViewUnpublished,
                    PermissionKey.Play_ChooseChallengeVariant,
                    PermissionKey.Play_IgnoreExecutionWindow,
                    PermissionKey.Play_IgnoreSessionResetSettings,
                ]
            },
            { UserRoleKey.Member, [] }
        };
    }
}
