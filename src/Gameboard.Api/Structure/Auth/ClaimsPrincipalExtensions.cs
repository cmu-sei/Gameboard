// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.Auth;

namespace Gameboard.Api;

public static class ClaimsPrincipalExtensions
{
    public static async Task<User> ToActor(this ClaimsPrincipal principal, IUserRolePermissionsService userRolePermissionsService)
    {
        // the user could be anon and therefore have no role claim (we distinguish "member" from "unauthed")
        var finalRole = default(UserRoleKey?);
        var roleString = principal.FindFirstValue(AppConstants.RoleClaimName);

        if (Enum.TryParse<UserRoleKey>(roleString, out var role))
            finalRole = role;

        return new User
        {
            Id = principal.Subject(),
            Name = principal.FindFirstValue(AppConstants.NameClaimName),
            ApprovedName = principal.FindFirstValue(AppConstants.ApprovedNameClaimName),
            Role = finalRole ?? UserRoleKey.Member,
            RolePermissions = await userRolePermissionsService.GetPermissions(role),
            SponsorId = principal.FindFirstValue(AppConstants.SponsorClaimName)
        };
    }

    public static string ToAuthenticatedGraderForChallengeId(this ClaimsPrincipal principal)
    {
        if (!principal.HasClaim(claim => claim.Type == GraderKeyAuthentication.GraderKeyChallengeIdClaimName))
            return null;

        return principal.FindFirstValue(GraderKeyAuthentication.GraderKeyChallengeIdClaimName);
    }

    public static string Subject(this ClaimsPrincipal principal)
    {
        var appSubjectClaim = principal.FindFirstValue(AppConstants.SubjectClaimName);
        return string.IsNullOrWhiteSpace(appSubjectClaim) ? principal.FindFirstValue(ClaimTypes.NameIdentifier) : appSubjectClaim;
    }

    public static string Name(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AppConstants.NameClaimName);
    }
}
