// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api;

public class User : IUserViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string ApprovedName { get; set; }
    public UserRole Role { get; set; }
    public Player[] Enrollments { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public int LoginCount { get; set; }

    public bool HasDefaultSponsor { get; set; }
    public string SponsorId { get; set; }
    public Sponsor Sponsor { get; set; }

    public bool IsAdmin => Role.HasFlag(UserRole.Admin);
    public bool IsDirector => Role.HasFlag(UserRole.Director);
    public bool IsRegistrar => Role.HasFlag(UserRole.Registrar);
    public bool IsDesigner => Role.HasFlag(UserRole.Designer);
    public bool IsTester => Role.HasFlag(UserRole.Tester);
    public bool IsObserver => Role.HasFlag(UserRole.Observer);
    public bool IsSupport => Role.HasFlag(UserRole.Support);
}

public class NewUser
{
    public required string Id { get; set; }
    public string SponsorId { get; set; }
    public bool UnsetDefaultSponsorFlag { get; set; }
}

public class ChangedUser
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string ApprovedName { get; set; }
    public string SponsorId { get; set; }
    public UserRole? Role { get; set; }
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
    public const string UserRoleFilter = "roles";
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

public class UserSimple : IUserViewModel
{
    public string Id { get; set; }
    public string ApprovedName { get; set; }
}

public class UserOnly : IUserViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string ApprovedName { get; set; }
    public Sponsor Sponsor { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public int LoginCount { get; set; }
    public UserRole Role { get; set; }
}

public interface IUserViewModel
{
    public string Id { get; set; }
    public string ApprovedName { get; set; }
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
    public required string SponsorId { get; set; }
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
    public required SimpleEntity Sponsor { get; set; }
    public required bool IsNewUser { get; set; }
}
