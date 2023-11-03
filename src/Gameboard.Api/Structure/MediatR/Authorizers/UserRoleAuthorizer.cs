using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal class UserRoleAuthorizer : IAuthorizer
{
    private readonly User _actor;
    private IEnumerable<UserRole> _allowedRoles { get; set; } = new List<UserRole> { UserRole.Admin };
    private string _allowUserId;

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

    public UserRoleAuthorizer AllowUserId(string userId)
    {
        _allowUserId = userId;
        return this;
    }

    public bool WouldAuthorize()
    {
        if (_allowUserId.NotEmpty() && _actor.Id == _allowUserId)
            return true;

        foreach (var role in _allowedRoles)
        {
            if (_actor.Role.HasFlag(role))
            {
                return true;
            }
        }

        return false;
    }

    public void Authorize()
    {
        if (!WouldAuthorize())
            throw new ActionForbidden();
    }
}
