// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api;

public class UserClaimTransformation
(
    IMemoryCache cache,
    IMapper mapper,
    ISponsorService sponsorService,
    IStore store,
    IUserRolePermissionsService userRolePermissionsService
) : IClaimsTransformation
{
    private readonly IMapper _mapper = mapper;
    private readonly IMemoryCache _cache = cache;
    private readonly ISponsorService _sponsorService = sponsorService;
    private readonly IStore _store = store;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // subject will be null if the user is authenticating with the GraderKey scheme
        // in this case, just pass the claims we already have
        var subject = principal.Subject();
        if (subject is null)
            return principal;

        if (!_cache.TryGetValue(subject, out User user))
        {
            user = _mapper.Map<User>
            (
                await _store
                    .WithNoTracking<Data.User>()
                    .SingleOrDefaultAsync(u => u.Id == subject, CancellationToken.None)
            );

            if (user is not null)
            {
                user.RolePermissions = await userRolePermissionsService.GetPermissions(user.Role);
            }
            else
            {
                user = new User
                {
                    Id = subject,
                    Role = UserRoleKey.Member,
                    RolePermissions = await userRolePermissionsService.GetPermissions(UserRoleKey.Member)
                };
            }

            if (user.SponsorId is null || user.SponsorId.IsEmpty())
            {
                var defaultSponsor = await _sponsorService.GetDefaultSponsor();
                user.SponsorId = defaultSponsor.Id;
            }

            // TODO: implement IChangeToken for this
            _cache.Set(subject, user, new TimeSpan(0, 5, 0));
        }

        var claims = new List<Claim>
        {
            new(AppConstants.SubjectClaimName, user.Id),
            new(AppConstants.NameClaimName, user.Name ?? ""),
            new(AppConstants.ApprovedNameClaimName, user.ApprovedName ?? ""),
            new(AppConstants.RoleClaimName, user.Role.ToString()),
            new(AppConstants.SponsorClaimName, user.SponsorId)
        };

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
}
