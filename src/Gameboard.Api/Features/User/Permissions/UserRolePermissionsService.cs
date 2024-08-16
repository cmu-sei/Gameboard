using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public interface IUserRolePermissionsService
{
    Task<IDictionary<UserRolePermissionKey, bool>> GetPermissions(UserRole role);
    Task<IDictionary<UserRolePermissionKey, bool>> GetPermissions(string userId, CancellationToken cancellationToken);
}

internal class UserRolePermissionsService(IStore _store) : IUserRolePermissionsService
{
    private static IDictionary<UserRole, UserRolePermissionKey[]> _rolePermissions = new Dictionary<UserRole, UserRolePermissionKey[]>()
    {
        {
            UserRole.Admin,
            Enum.GetValues<UserRolePermissionKey>()
        },
        {
            UserRole.Designer,
            [
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.Games_ConfigureChallenges,
                UserRolePermissionKey.Games_CreateEditDelete,
                UserRolePermissionKey.Observe,
                UserRolePermissionKey.Play_IgnoreExecutionWindow,
                UserRolePermissionKey.Play_IgnoreSessionResetSettings,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Players_ViewSubmissions,
                UserRolePermissionKey.Reports_View,
                UserRolePermissionKey.Players_ViewSubmissions,
                UserRolePermissionKey.Scores_ViewLive
            ]
        },
        {
            UserRole.Director,
            [
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.EventHorizon_View,
                UserRolePermissionKey.Observe,
                UserRolePermissionKey.Play_IgnoreExecutionWindow,
                UserRolePermissionKey.Play_IgnoreSessionResetSettings,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Reports_View,
                UserRolePermissionKey.Scores_ViewLive
            ]
        },
        {
            UserRole.Observer,
            [
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.EventHorizon_View,
                UserRolePermissionKey.Observe,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Scores_ViewLive
            ]
        },
        {
            UserRole.Registrar,
            [
                UserRolePermissionKey.Games_AdminExternal,
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.Games_EnrollPlayers,
                UserRolePermissionKey.Play_IgnoreExecutionWindow,
                UserRolePermissionKey.Play_IgnoreSessionResetSettings,
                UserRolePermissionKey.Players_ApproveNameChanges,
                UserRolePermissionKey.Players_EditSession,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Users_Create
            ]
        },
        {
            UserRole.Support,
            [
                UserRolePermissionKey.Games_AdminExternal,
                UserRolePermissionKey.Players_ApproveNameChanges,
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.EventHorizon_View,
                UserRolePermissionKey.Games_EnrollPlayers,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Players_ViewSubmissions,
                UserRolePermissionKey.Reports_View,
                UserRolePermissionKey.Support_EditSettings,
                UserRolePermissionKey.Support_ManageTickets,
                UserRolePermissionKey.Scores_ViewLive,
                UserRolePermissionKey.SystemNotifications_CreateEdit,
            ]
        },
        {
            UserRole.Tester,
            [
                UserRolePermissionKey.Games_AdminExternal,
                UserRolePermissionKey.Players_ApproveNameChanges,
                UserRolePermissionKey.Admin_View,
                UserRolePermissionKey.Games_EnrollPlayers,
                UserRolePermissionKey.Play_IgnoreExecutionWindow,
                UserRolePermissionKey.Play_IgnoreSessionResetSettings,
                UserRolePermissionKey.Players_ViewActiveChallenges,
                UserRolePermissionKey.Scores_ViewLive
            ]
        },
        { UserRole.Member, Array.Empty<UserRolePermissionKey>() }
    };

    public Task<IDictionary<UserRolePermissionKey, bool>> GetPermissions(UserRole role)
    {
        var permissions = _rolePermissions[role];
        var things = Enum.GetValues<UserRolePermissionKey>();

        return Task.FromResult
        (
            Enum
                .GetValues<UserRolePermissionKey>()
                .Distinct()
                .ToDictionary(p => p, p => permissions.Contains(p))
                as IDictionary<UserRolePermissionKey, bool>
        );
    }

    public async Task<IDictionary<UserRolePermissionKey, bool>> GetPermissions(string userId, CancellationToken cancellationToken)
    {
        var role = await _store
            .WithNoTracking<Data.User>()
            .Select(u => u.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return await GetPermissions(role);
    }
}
