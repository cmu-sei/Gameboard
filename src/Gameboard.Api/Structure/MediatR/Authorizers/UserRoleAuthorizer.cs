using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal class UserRoleAuthorizer : IAuthorizer
{
    private User _actor;
    private IEnumerable<UserRole> _allowedRoles { get; set; } = new List<UserRole> { UserRole.Admin };

    public UserRoleAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor
            .HttpContext
            .User
            .ToActor();
    }

    public UserRoleAuthorizer AllowRoles(params UserRole[] roles)
    {
        _allowedRoles = roles;
        return this;
    }

    public void Authorize()
    {
        foreach (var role in _allowedRoles)
        {
            if (_actor.Role.HasFlag(role))
            {
                return;
            }
        }

        throw new ActionForbidden();
    }
}
