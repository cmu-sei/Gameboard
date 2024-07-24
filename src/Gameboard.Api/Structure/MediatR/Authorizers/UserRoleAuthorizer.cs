using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal class UserRoleAuthorizer : IAuthorizer
{
    private readonly IActingUserService _actingUserService;
    private IEnumerable<UserRole> _allowedRoles = new List<UserRole> { UserRole.Admin };
    private readonly List<string> _allowUserIds = new();

    public UserRoleAuthorizer(IActingUserService actingUserService)
    {
        _actingUserService = actingUserService;
    }

    public UserRoleAuthorizer AllowRoles(params UserRole[] roles)
    {
        _allowedRoles = roles;
        return this;
    }

    public UserRoleAuthorizer AllowAllElevatedRoles()
    {
        _allowedRoles = new List<UserRole>
        {
            UserRole.Admin,
            UserRole.Designer,
            UserRole.Director,
            UserRole.Observer,
            UserRole.Registrar,
            UserRole.Support,
            UserRole.Tester
        };

        return this;
    }

    public UserRoleAuthorizer AllowUserId(string userId)
    {
        _allowUserIds.Add(userId);
        return this;
    }

    public UserRoleAuthorizer AllowUserIds(params string[] userIds)
    {
        _allowUserIds.AddRange(userIds);
        return this;
    }

    public bool WouldAuthorize()
    {
        var actingUser = _actingUserService.Get();
        if (_allowUserIds.Any() && _allowUserIds.Contains(actingUser.Id))
            return true;

        foreach (var role in _allowedRoles)
        {
            if (actingUser.Role.HasFlag(role))
                return true;
        }

        return false;
    }

    public void Authorize()
    {
        if (!WouldAuthorize())
            throw new ActionForbidden();
    }
}
