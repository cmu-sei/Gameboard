using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal class UserRoleAuthorizer : IAuthorizer
{
    private readonly User _actor;
    public IEnumerable<UserRole> AllowedRoles { get; set; } = new List<UserRole> { UserRole.Admin };

    public UserRoleAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor
            .HttpContext
            .User
            .ToActor();
    }

    public void Authorize()
    {
        foreach (var role in AllowedRoles)
        {
            if (_actor.Role.HasFlag(role))
                return;
        }

        throw new ActionForbidden();
    }
}
