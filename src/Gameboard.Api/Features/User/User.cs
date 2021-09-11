// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;

namespace Gameboard.Api
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameStatus { get; set; }
        public string ApprovedName { get; set; }
        public string Sponsor { get; set; }
        public UserRole Role { get; set; }
        public Player[] Enrollments { get; set; }
        public bool IsAdmin => Role.HasFlag(UserRole.Admin);
        public bool IsDirector => Role.HasFlag(UserRole.Director);
        public bool IsRegistrar => Role.HasFlag(UserRole.Registrar);
        public bool IsDesigner => Role.HasFlag(UserRole.Designer);
        public bool IsTester => Role.HasFlag(UserRole.Tester);
        public bool IsObserver => Role.HasFlag(UserRole.Observer);
    }

    public class NewUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sponsor { get; set; }
        // public string Username { get; set; }
        // public string Email { get; set; }
    }

    public class ChangedUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameStatus { get; set; }
        public string ApprovedName { get; set; }
        public string Sponsor { get; set; }
        public UserRole Role { get; set; }
    }

    public class SelfChangedUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sponsor { get; set; }
    }

    public class TeamMember
    {
        public string Id { get; set; }
        public string ApprovedName { get; set; }
        public PlayerRole Role { get; set; }
    }

    public class UserSearch: SearchFilter
    {
        public const string UserRoleFilter = "roles";
        public const string NamePendingFilter = "pending";
        public const string NameDisallowedFilter = "disallowed";
        public bool WantsRoles => Filter.Contains(UserRoleFilter);
        public bool WantsPending => Filter.Contains(NamePendingFilter);
        public bool WantsDisallowed => Filter.Contains(NameDisallowedFilter);
    }

    public class Announcement
    {
        public string TeamId { get; set; }
        public string Message { get; set; }
    }
}
