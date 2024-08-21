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
    Task<IDictionary<UserRole, IEnumerable<PermissionKey>>> GetAllRolePermissions();
    Task<IDictionary<PermissionKey, bool>> GetPermissions(UserRole role);
    Task<IDictionary<PermissionKey, bool>> GetPermissions(string userId, CancellationToken cancellationToken);
    Task<IEnumerable<UserRolePermission>> List();
    UserRole ResolveSingle(UserRole role);
}

internal class UserRolePermissionsService(IStore _store) : IUserRolePermissionsService
{
    private static readonly IEnumerable<UserRolePermission> _permissions =
    [
        new()
        {
            Group = PermissionKeyGroup.Admin,
            Key = PermissionKey.Admin_View,
            Name = "Admin Area",
            Description = "Access the Admin area"
        },
        new()
        {
            Group = PermissionKeyGroup.Admin,
            Key = PermissionKey.Admin_CreateEditSponsors,
            Name = "Create/edit sponsors",
            Description = "Create and edit sponsor organizations"
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_AdminExternal,
            Name = "Administer external games",
            Description = "Administer and deploy external-mode games"
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_ConfigureChallenges,
            Name = "Configure challenges",
            Description = "Add and remove challenges from games and set their scoring properties"
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_CreateEditDelete,
            Name = "Create/edit/delete games",
            Description = "Create, edit, and delete games"
        },
        new()
        {
            Group = PermissionKeyGroup.Play,
            Key = PermissionKey.Play_IgnoreExecutionWindow,
            Name = "Ignore registration/execution windows",
            Description = "Ignore registration and execution window settings when enrolling in and starting games"
        },
        new()
        {
            Group = PermissionKeyGroup.Play,
            Key = PermissionKey.Play_IgnoreSessionResetSettings,
            Name = "Ignore session reset settings",
            Description = "Reset their session, even in games where session reset is prohibited"
        },
        new()
        {
            Group = PermissionKeyGroup.Practice,
            Key = PermissionKey.Practice_EditSettings,
            Name = "Practice Area",
            Description = "Edit settings for the Practice Area"
        },
        new()
        {
            Group = PermissionKeyGroup.Reports,
            Key = PermissionKey.Reports_View,
            Name = "Reports",
            Description = "Run, view, and share reports"
        },
        new()
        {
            Group = PermissionKeyGroup.Scoring,
            Key = PermissionKey.Scores_AwardManualBonuses,
            Name = "Award manual bonuses",
            Description = "Award manual bonuses to individual players or teams"
        },
        new()
        {
            Group = PermissionKeyGroup.Scoring,
            Key = PermissionKey.Scores_ViewLive,
            Name = "View scores live",
            Description = "View scores for all players and teams (even before the game has ended)"
        },
        new()
        {
            Group = PermissionKeyGroup.Support,
            Key = PermissionKey.Support_EditSettings,
            Name = "Edit Support settings",
            Description = "Edit support settings (e.g. support page greeting)"
        },
        new()
        {
            Group = PermissionKeyGroup.Support,
            Key = PermissionKey.Support_ManageTickets,
            Name = "Manage tickets",
            Description = "Manage, edit, assign, and respond to tickets"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_ApproveNameChanges,
            Name = "Approve name changes",
            Description = "Approve name change requests for users and players"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_DeployGameResources,
            Name = "Deploy game resources",
            Description = "Deploy virtual resources on behalf of players through the Admin section"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_EditSession,
            Name = "Administer sessions",
            Description = "Manually end and extend team play sessions"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_Enroll,
            Name = "Enroll Players",
            Description = "Enroll players in games on their behalf"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_Observe,
            Name = "Observe",
            Description = "See information about all active challenges and teams"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_SendAnnouncements,
            Name = "Send announcements",
            Description = "Send announcements to all players (or individual players and teams)"
        },
        new()
        {
            Group = PermissionKeyGroup.Users,
            Key = PermissionKey.Users_Create,
            Name = "Create users manually",
            Description = "Create users manually (currently available only as an API call)"
        },
    ];

    private static readonly IDictionary<UserRole, PermissionKey[]> _rolePermissions = new Dictionary<UserRole, PermissionKey[]>()
    {
        {
            UserRole.Admin,
            Enum.GetValues<PermissionKey>()
        },
        {
            UserRole.Designer,
            [
                PermissionKey.Admin_View,
                PermissionKey.Games_ConfigureChallenges,
                PermissionKey.Games_CreateEditDelete,
                PermissionKey.Play_IgnoreExecutionWindow,
                PermissionKey.Play_IgnoreSessionResetSettings,
                PermissionKey.Reports_View,
                PermissionKey.Scores_ViewLive,
                PermissionKey.Teams_Observe,
            ]
        },
        {
            UserRole.Director,
            [
                PermissionKey.Admin_View,
                PermissionKey.Play_IgnoreExecutionWindow,
                PermissionKey.Play_IgnoreSessionResetSettings,
                PermissionKey.Reports_View,
                PermissionKey.Scores_ViewLive,
                PermissionKey.Teams_Observe
            ]
        },
        {
            UserRole.Observer,
            [
                PermissionKey.Admin_View,
                PermissionKey.Scores_ViewLive,
                PermissionKey.Teams_Observe,
            ]
        },
        {
            UserRole.Registrar,
            [
                PermissionKey.Admin_View,
                PermissionKey.Games_AdminExternal,
                PermissionKey.Play_IgnoreExecutionWindow,
                PermissionKey.Play_IgnoreSessionResetSettings,
                PermissionKey.Teams_ApproveNameChanges,
                PermissionKey.Teams_EditSession,
                PermissionKey.Teams_Enroll,
                PermissionKey.Users_Create
            ]
        },
        {
            UserRole.Support,
            [
                PermissionKey.Admin_View,
                PermissionKey.Games_AdminExternal,
                PermissionKey.Reports_View,
                PermissionKey.Scores_ViewLive,
                PermissionKey.Support_EditSettings,
                PermissionKey.Support_ManageTickets,
                PermissionKey.SystemNotifications_CreateEdit,
                PermissionKey.Teams_ApproveNameChanges,
                PermissionKey.Teams_Enroll,
            ]
        },
        {
            UserRole.Tester,
            [
                PermissionKey.Admin_View,
                PermissionKey.Games_AdminExternal,
                PermissionKey.Play_IgnoreExecutionWindow,
                PermissionKey.Play_IgnoreSessionResetSettings,
                PermissionKey.Scores_ViewLive,
                PermissionKey.Teams_ApproveNameChanges,
                PermissionKey.Teams_Enroll,
            ]
        },
        { UserRole.Member, Array.Empty<PermissionKey>() }
    };

    public Task<IDictionary<PermissionKey, bool>> GetPermissions(UserRole role)
    {
        var permissions = _rolePermissions[ResolveSingle(role)];

        return Task.FromResult
        (
            Enum
                .GetValues<PermissionKey>()
                .Distinct()
                .ToDictionary(p => p, p => permissions.Contains(p))
                as IDictionary<PermissionKey, bool>
        );
    }

    public async Task<IDictionary<PermissionKey, bool>> GetPermissions(string userId, CancellationToken cancellationToken)
    {
        var role = await _store
            .WithNoTracking<Data.User>()
            .Select(u => u.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return await GetPermissions(role);
    }

    public Task<IEnumerable<UserRolePermission>> List()
        => Task.FromResult(_permissions);

    public async Task<IDictionary<UserRole, IEnumerable<PermissionKey>>> GetAllRolePermissions()
    {
        var retVal = new Dictionary<UserRole, IEnumerable<PermissionKey>>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var rolePermissions = await GetPermissions(ResolveSingle(role));
            retVal.Add(role, rolePermissions.Where(r => r.Value).Select(r => r.Key));
        }

        return retVal;
    }

    /// <summary>
    /// Hack until we decide the real hierarchy of roles
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public UserRole ResolveSingle(UserRole role)
    {
        if (role.HasFlag(UserRole.Admin))
            return UserRole.Admin;

        if (role.HasFlag(UserRole.Support))
            return UserRole.Support;

        if (role.HasFlag(UserRole.Tester))
            return UserRole.Tester;

        if (role.HasFlag(UserRole.Registrar))
            return UserRole.Registrar;

        if (role.HasFlag(UserRole.Designer))
            return UserRole.Designer;

        if (role.HasFlag(UserRole.Director))
            return UserRole.Director;

        return UserRole.Member;
    }
}
