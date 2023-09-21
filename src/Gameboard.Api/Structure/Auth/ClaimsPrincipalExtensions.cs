// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Security.Claims;
using Gameboard.Api.Structure.Auth;

namespace Gameboard.Api;

public static class ClaimsPrincipalExtension
{
    public static User ToActor(this ClaimsPrincipal principal)
    {
        return new User
        {
            Id = principal.FindFirstValue(AppConstants.SubjectClaimName),
            Name = principal.FindFirstValue(AppConstants.NameClaimName),
            ApprovedName = principal.FindFirstValue(AppConstants.ApprovedNameClaimName),
            Role = Enum.Parse<UserRole>(principal.FindFirstValue(AppConstants.RoleListClaimName) ?? UserRole.Member.ToString()),
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
        return principal.FindFirstValue(AppConstants.SubjectClaimName);
    }

    public static string Name(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AppConstants.NameClaimName);
    }
}
