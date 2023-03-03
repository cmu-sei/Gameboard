using System;
using System.Collections.Generic;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal class UserRoleAuthorizer : IAuthorizer
{
    public IEnumerable<UserRole> AllowedRoles { get; set; } = new List<UserRole> { UserRole.Admin };

    public bool Authorize(User actor)
    {
        foreach (var role in AllowedRoles)
        {
            if (actor.Role.HasFlag(role))
            {
                return true;
            }
        }

        return false;
    }
}
