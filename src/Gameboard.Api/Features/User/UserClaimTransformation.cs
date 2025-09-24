// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.Auth.Crucible;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api;

public class UserClaimTransformation
(
    ILogger<UserClaimTransformation> logger,
    IMemoryCache cache,
    IMapper mapper,
    OidcOptions oidcOptions,
    ISponsorService sponsorService,
    IStore store,
    IUserRolePermissionsService userRolePermissionsService
) : IClaimsTransformation
{
    private readonly IMapper _mapper = mapper;
    private readonly IMemoryCache _cache = cache;
    private readonly OidcOptions _oidcOptions = oidcOptions;
    private readonly ISponsorService _sponsorService = sponsorService;
    private readonly IStore _store = store;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // subject will be null if the user is authenticating with the GraderKey scheme
        // in this case, just pass the claims we already have
        var subject = principal.Subject();
        if (subject is null)
        {
            return principal;
        }

        if (!_cache.TryGetValue(subject, out User user))
        {
            user = _mapper.Map<User>
            (
                await _store
                    .WithNoTracking<Data.User>()
                    .SingleOrDefaultAsync(u => u.Id == subject, CancellationToken.None)
            );

            // handle unseen user/subject
            user ??= new User
            {
                Id = subject,
                Role = UserRoleKey.Member
            };

            if (user.SponsorId.IsEmpty())
            {
                var defaultSponsor = await _sponsorService.GetDefaultSponsor();
                user.SponsorId = defaultSponsor.Id;
            }

            if (_oidcOptions.DefaultUserNameClaimType.IsNotEmpty())
            {
                var claimValue = principal.FindFirstValue(_oidcOptions.DefaultUserNameClaimType);

                if (claimValue.IsNotEmpty())
                {
                    user.ApprovedName = claimValue;
                    user.Name = string.Empty;
                }
            }

            if (_oidcOptions.StoreUserEmails || _oidcOptions.DefaultUserNameInferFromEmail)
            {
                var emailClaimValue = principal.FindFirstValue(AppConstants.EmailClaimName);

                if (_oidcOptions.StoreUserEmails)
                {
                    if (emailClaimValue.IsNotEmpty())
                    {
                        user.Email = emailClaimValue;
                    }
                }

                if (_oidcOptions.DefaultUserNameInferFromEmail && emailClaimValue.IsNotEmpty() && user.ApprovedName.IsEmpty())
                {
                    var atIndex = emailClaimValue.IndexOf('@');
                    if (atIndex >= 0)
                    {
                        user.ApprovedName = emailClaimValue[..atIndex];
                    }
                }
            }

            // Resolve the user's role by examining their app-level role and any IDP-supplied roles (see function below)
            user.Role = await ResolveUserRole(principal, user.Id, user.Role);
            // And resolve their permissions based on role
            user.RolePermissions = await userRolePermissionsService.GetPermissions(user.Role);

            // TODO: implement IChangeToken for this
            _cache.Set(subject, user, new TimeSpan(0, 5, 0));
        }

        var claims = new List<Claim>
        {
            new(AppConstants.SubjectClaimName, user.Id),
            new(AppConstants.NameClaimName, user.Name ?? string.Empty),
            new(AppConstants.ApprovedNameClaimName, user.ApprovedName ?? string.Empty),
            new(AppConstants.RoleClaimName, user.Role.ToString()),
            new(AppConstants.SponsorClaimName, user.SponsorId)
        };

        if (user.Email.IsNotEmpty())
        {
            claims.Add(new(AppConstants.EmailClaimName, user.Email));
        }

        return new ClaimsPrincipal
        (
            new ClaimsIdentity
            (
                claims,
                principal.Identity.AuthenticationType,
                AppConstants.NameClaimName,
                AppConstants.RoleClaimName
            )
        );
    }

    /// <summary>
    /// The user resolved from the ClaimsPrincipal may have an app-level (Gameboard) role, one or more identity provider-assigned roles,
    /// or both. If they have an IDP role, we need to resolve it and log it in the DB. The user's effective role (which will be set to the Role claim)
    /// during claims transformation is the "highest" among all roles between their GB role and any resolved IDP roles.
    /// 
    /// To configure mapping between 
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal representing the current user being transformed.</param>
    /// <param name="appUserId">The ID of the user currently being transformed</param>
    /// <param name="appUserRole">The app-level role of the user currently being transformed</param>
    /// <returns>The user's effective role, defined as the "maximum" role they have among Gameboard and all IDP roles (if they exist).</returns>
    private async Task<UserRoleKey> ResolveUserRole(ClaimsPrincipal principal, string appUserId, UserRoleKey appUserRole)
    {
        logger.LogInformation("Resolving user role (user {appUserId}, app role {appUserRole})", appUserId, appUserRole);

        // encapsulate the computation of their highest IDP role, if it exists
        var idpRole = ResolveUserIdpRole(principal);
        logger.LogInformation("User {appUser} has IDP role {idpRole}", appUserId, idpRole);

        // if we resolved an IDP role, update the DB so we can track and display that information (to explain)
        // conflicts between a user's GB-app-level role and their IDP assigned role
        if (idpRole is not null)
        {
            await _store
                .WithNoTracking<Data.User>()
                .Where(u => u.Id == appUserId)
                .ExecuteUpdateAsync(up => up.SetProperty(u => u.LastIdpAssignedRole, idpRole));
        }

        var effectiveRole = idpRole is not null && idpRole > appUserRole ? idpRole.Value : appUserRole;
        logger.LogInformation("User {userId} effective role resolved to {role}", appUserId, effectiveRole);
        return effectiveRole;
    }

    private UserRoleKey? ResolveUserIdpRole(ClaimsPrincipal principal)
    {
        if (_oidcOptions.UserRolesClaimPath.IsEmpty() || _oidcOptions.UserRolesClaimMap.Count == 0)
        {
            return null;
        }

        // if role claim path is set and we have a map from its value to a GB user role, they get the "highest" of all 
        // possible roles among their GB role and IDP roles
        var roleClaimValues = principal.GetRoleClaims(_oidcOptions.UserRolesClaimPath);
        var idpResolvedUserRoles = new List<UserRoleKey>();

        foreach (var value in roleClaimValues)
        {
            if (_oidcOptions.UserRolesClaimMap.TryGetValue(value, out var idpRoleMapping))
            {
                // if the string value that this claim is mapped to matches the name of a GB user role, 
                // add it to the possible list of roles we can resolve from the IDP
                if (Enum.TryParse<UserRoleKey>(idpRoleMapping, out var userRoleKey))
                {
                    idpResolvedUserRoles.Add(userRoleKey);
                }
            }
        }

        if (idpResolvedUserRoles.Count == 0)
        {
            return null;
        }

        return idpResolvedUserRoles.Max();
    }
}
