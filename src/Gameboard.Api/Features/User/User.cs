// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;

namespace Gameboard.Api;

public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string ApprovedName { get; set; }
    public UserRoleKey Role { get; set; }
    public IEnumerable<PermissionKey> RolePermissions { get; set; } = [];
    public string Email { get; set; }
    public Player[] Enrollments { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public UserRoleKey? LastIdpAssignedRole { get; set; }
    public int LoginCount { get; set; }

    public bool HasDefaultSponsor { get; set; }
    public string SponsorId { get; set; }
    public Sponsor Sponsor { get; set; }
}

public class NewUser
{
    public required string Id { get; set; }
    public string DefaultName { get; set; }
    public UserRoleKey Role { get; set; } = UserRoleKey.Member;
    public string SponsorId { get; set; }
    public bool UnsetDefaultSponsorFlag { get; set; }
}

public class UpdateUser
{
    public string Id { get; set; }
    public string SponsorId { get; set; }
    public UserRoleKey? Role { get; set; }
    public bool? PlayAudioOnBrowserNotification { get; set; }
}

public class SelfChangedUser
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SponsorId { get; set; }
}

public class TeamMember
{
    public required string Id { get; set; }
    public required string ApprovedName { get; set; }
    public required PlayerRole Role { get; set; }
    public required string UserId { get; set; }
}

public class UserSearch : SearchFilter
{
    public const string UserRoleFilter = "elevated-role";
    public const string NamePendingFilter = "pending";
    public const string NameDisallowedFilter = "disallowed";
    public string EligibleForGameId { get; set; }
    public string ExcludeIds { get; set; }
    public bool WantsRoles => Filter.Contains(UserRoleFilter);
    public bool WantsPending => Filter.Contains(NamePendingFilter);
    public bool WantsDisallowed => Filter.Contains(NameDisallowedFilter);
}

public class UserSettings
{
    public bool PlayAudioOnBrowserNotification { get; set; }
}

public class ListUsersResponseUser
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string NameStatus { get; set; }
    public required string ApprovedName { get; set; }
    public required SponsorWithParentSponsor Sponsor { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public required DateTimeOffset? LastLoginDate { get; set; }
    public required int LoginCount { get; set; }

    // role madness
    public required UserRoleKey AppRole { get; set; }
    public required UserRoleKey? LastIdpAssignedRole { get; set; }
    public required UserRoleKey EffectiveRole { get; set; }
}

public class TryCreateUserResult
{
    public required bool IsNewUser { get; set; }
    public User User { get; set; }
}

public class TryCreateUsersRequest
{
    public required bool AllowSubsetCreation { get; set; }
    public string EnrollInGameId { get; set; }
    [TypeConverter(typeof(JsonStringEnumConverter<UserRoleKey>))]
    public UserRoleKey? Role { get; set; } = UserRoleKey.Member;
    public string SponsorId { get; set; }
    public required bool UnsetDefaultSponsorFlag { get; set; }
    public required IEnumerable<string> UserIds { get; set; }
}

public sealed class TryCreateUsersResponse
{
    public required IEnumerable<TryCreateUsersResponseUser> Users { get; set; }
}

public sealed class TryCreateUsersResponseUser
{
    public required string Id { get; set; }
    public required string EnrolledInGameId { get; set; }
    public required string Name { get; set; }
    public required UserRoleKey Role { get; set; }
    public required SimpleEntity Sponsor { get; set; }
    public required bool IsNewUser { get; set; }
}
