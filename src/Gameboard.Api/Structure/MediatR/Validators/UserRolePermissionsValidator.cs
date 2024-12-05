using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;

namespace Gameboard.Api.Structure.MediatR.Validators;

public interface IUserRolePermissionsValidator
{
    public IUserRolePermissionsValidator RequireAuthentication();
    public IUserRolePermissionsValidator RequirePermissions(params PermissionKey[] requiredPermissions);
    public IUserRolePermissionsValidator Unless(Func<Task<bool>> condition);
    public IUserRolePermissionsValidator Unless(Func<Task<bool>> condition, GameboardValidationException validationException);
    public IUserRolePermissionsValidator UnlessUserIdIn(params string[] userIds);
}

internal class UserRolePermissionsValidator(IUserRolePermissionsService userRolePermissionsService) : IUserRolePermissionsValidator
{
    private bool _requireAuthentication = false;
    private readonly IUserRolePermissionsService _userRolePermissionsService = userRolePermissionsService;
    private IEnumerable<PermissionKey> _requiredPermissions { get; set; } = [];
    private Func<Task<bool>> _unless { get; set; }
    private GameboardValidationException _unlessException;
    private IEnumerable<string> _unlessUserIdIn { get; set; } = [];

    internal async Task<IEnumerable<GameboardValidationException>> GetAuthValidationExceptions(User user)
    {
        if (_requireAuthentication && user is null)
            throw new UnauthorizedAccessException($"This operation requires authentication.");

        // if there are no required permissions, validation always passes
        if (_requiredPermissions is not null && _requiredPermissions.Any())
        {
            // if the user is on the whitelist, let em through
            if (_unlessUserIdIn is not null && _unlessUserIdIn.Any(uId => uId == user.Id))
                return [];

            // if some other condition allows access independent of user ID
            if (_unless is not null)
            {
                var result = await _unless();

                if (result)
                {
                    return [];
                }
            }

            // otherwise, check their role to see if it has the permissions needed
            var permissions = await _userRolePermissionsService.GetPermissions(user.Role);
            var missingPermissions = _requiredPermissions.Where(p => !permissions.Contains(p));

            var retVal = new List<GameboardValidationException>();

            if (missingPermissions.Any())
            {
                retVal.Add(new UserRolePermissionException(user.Role, missingPermissions));

                if (_unless is not null && _unlessException is not null)
                {
                    retVal.Add(_unlessException);
                }
            }

            return retVal;
        }

        return [];
    }

    public IUserRolePermissionsValidator RequireAuthentication()
    {
        _requireAuthentication = true;
        return this;
    }

    public IUserRolePermissionsValidator RequirePermissions(params PermissionKey[] requiredPermissions)
    {
        _requiredPermissions = requiredPermissions;
        return this;
    }

    public IUserRolePermissionsValidator Unless(Func<Task<bool>> condition)
    {
        _unless = condition;
        return this;
    }

    public IUserRolePermissionsValidator Unless(Func<Task<bool>> condition, GameboardValidationException validationException)
    {
        Unless(condition);
        _unlessException = validationException;
        return this;
    }

    public IUserRolePermissionsValidator UnlessUserIdIn(params string[] userIds)
    {
        _unlessUserIdIn = userIds;
        return this;
    }
}
