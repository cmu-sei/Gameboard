using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public interface IUserRolePermissionsService
{
    Task<bool> Can(PermissionKey key);
    Task<bool> Can(UserRoleKey role, PermissionKey permissionKey);
    bool IsActingUser(string userId);
    Task<bool> IsActingUserAsync(string userId);
    Task<IEnumerable<PermissionKey>> GetAllPermissions();
    Task<IEnumerable<PermissionKey>> GetPermissions(UserRoleKey? role);
    Task<IEnumerable<PermissionKey>> GetPermissions(string userId, CancellationToken cancellationToken);
    Task<IEnumerable<UserRole>> GetRoles();
    Task<IEnumerable<UserRoleKey>> GetRolesWithPermission(PermissionKey key);
    Task<IEnumerable<UserRolePermission>> List();
}

internal class UserRolePermissionsService(IActingUserService actingUserService, IUserRolePermissionsConfigurationService configurationService, IStore store) : IUserRolePermissionsService
{
    private static readonly IEnumerable<UserRolePermission> _permissions =
    [
        new()
        {
            Group = PermissionKeyGroup.Admin,
            Key = PermissionKey.ApiKeys_CreateRevoke,
            Name = "Manage API Keys",
            Description = "Can generate API keys for any user and revoke their access"
        },
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
            Key = PermissionKey.SystemNotifications_CreateEdit,
            Name = "Manage system notifications",
            Description = "Create, edit, and delete notifications which appear at the top of the app"
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_CreateEditDelete,
            Name = "Create/edit/delete games",
            Description = "Create, edit, and delete games. Add and remove challenges, set their scoring properties, and add manual bonuses."
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_DeleteWithPlayerData,
            Name = "Delete played games",
            Description = "Delete _all_ games, including games which have registered, playing, or completed players."
        },
        new()
        {
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Games_ViewUnpublished,
            Name = "View hidden games and practice challenges",
            Description = "View games and practice challenges which have been hidden from players by their creator"
        },
        new()
        {
            Group = PermissionKeyGroup.Play,
            Key = PermissionKey.Play_ChooseChallengeVariant,
            Name = "Select challenge variants",
            Description = "Choose any variant of a challenge when deploying (rather than random assignment)"
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
            Key = PermissionKey.Scores_RegradeAndRerank,
            Name = "Revise scores",
            Description = "Manually initiate reranking of games and regrading of challenges"
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
            Group = PermissionKeyGroup.Admin,
            Key = PermissionKey.Sponsors_CreateEdit,
            Name = "Create/edit sponsors",
            Description = "Create and edit sponsor organizations"
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
            Group = PermissionKeyGroup.Support,
            Key = PermissionKey.Support_ViewTickets,
            Name = "View tickets",
            Description = "View all tickets in the app"
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
            Key = PermissionKey.Teams_CreateEditDeleteChallenges,
            Name = "Create/delete challenge instances",
            Description = "Start and purge an instance of a challenge on behalf of any team"
        },
        new()
        {
            Group = PermissionKeyGroup.Teams,
            Key = PermissionKey.Teams_DeployGameResources,
            Name = "Deploy game resources",
            Description = "Deploy virtual  on behalf of players through the Admin section"
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
            Group = PermissionKeyGroup.Games,
            Key = PermissionKey.Teams_SetSyncStartReady,
            Name = "Set players to ready",
            Description = "Change player status to ready/not ready in sync-start games"
        },
        new()
        {
            Group = PermissionKeyGroup.Users,
            Key = PermissionKey.Users_CreateEditDelete,
            Name = "Create users manually",
            Description = "Create and edit users manually (currently available only as an API call)"
        },
        new()
        {
            Group = PermissionKeyGroup.Users,
            Key = PermissionKey.Users_EditRoles,
            Name = "Assign roles",
            Description = "Assign roles to other users"
        }
    ];

    private readonly IUserRolePermissionsConfigurationService _configurationService = configurationService;
    private readonly IStore _store = store;

    public async Task<bool> Can(PermissionKey key)
    {
        var actingUser = actingUserService.Get();

        if (actingUser is null)
            return false;

        var rolePermissions = await GetPermissions(actingUserService.Get().Role);
        return rolePermissions.Contains(key);
    }

    public async Task<bool> Can(UserRoleKey role, PermissionKey permissionKey)
    {
        var rolesWithPermission = await GetRolesWithPermission(permissionKey);
        return rolesWithPermission.Contains(role);
    }

    public Task<IEnumerable<PermissionKey>> GetAllPermissions()
        => Task.FromResult<IEnumerable<PermissionKey>>(Enum.GetValues<PermissionKey>());

    public Task<IEnumerable<PermissionKey>> GetPermissions(UserRoleKey? role)
    {
        var permissions = Array.Empty<PermissionKey>() as IEnumerable<PermissionKey>;
        if (role is not null)
            permissions = _configurationService.GetConfiguration()[role.Value];

        return Task.FromResult(permissions);
    }

    public async Task<IEnumerable<PermissionKey>> GetPermissions(string userId, CancellationToken cancellationToken)
    {
        var role = await _store
            .WithNoTracking<Data.User>()
            .Select(u => u.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return await GetPermissions(role);
    }

    public async Task<IEnumerable<UserRole>> GetRoles()
    {
        var retVal = new List<UserRole>();
        var descriptions = await GetRoleDescriptions();

        foreach (var role in Enum.GetValues<UserRoleKey>())
        {
            retVal.Add(new UserRole
            {
                Key = role,
                Description = descriptions[role],
                Permissions = await GetPermissions(role)
            });
        }

        return retVal;
    }

    public Task<IDictionary<UserRoleKey, string>> GetRoleDescriptions()
    {
        return Task.FromResult(new Dictionary<UserRoleKey, string>
        {
            { UserRoleKey.Member, "General users of the application with no elevated permissions." },
            { UserRoleKey.Admin, "Full permissions to access all parts of the application and change permissions assigned to other users." },
            { UserRoleKey.Director, "**Tester** and **Support** permissions plus permission to create/edit/delete games/game settings, deploy game resources, manage team sessions and scores, and create/edit some site-wide settings (e.g., announcements, practice area, etc.)." },
            { UserRoleKey.Support, "**Tester** permissions plus permission to view additional information about teams/games (e.g., observe teams, manage enrolled players, etc.), view reports, and manage support tickets." },
            { UserRoleKey.Tester, "Can play games outside of the execution window and view hidden games for the purpose of testing game functionality." },
        } as IDictionary<UserRoleKey, string>);
    }

    public bool IsActingUser(string userId)
    {
        var actingUser = actingUserService.Get();
        return actingUser?.Id == userId;
    }

    public Task<bool> IsActingUserAsync(string userId)
        => Task.FromResult(IsActingUser(userId));

    public Task<IEnumerable<UserRolePermission>> List()
        => Task.FromResult(_permissions);

    public Task<IEnumerable<UserRoleKey>> GetRolesWithPermission(PermissionKey key)
    {
        var config = _configurationService.GetConfiguration();

        return Task.FromResult
        (
            config
                .Keys
                .Where(k => config[k].Contains(key))
                .ToArray() as IEnumerable<UserRoleKey>
        );
    }
}
