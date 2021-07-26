// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api
{
    public class UserClaimTransformation : IClaimsTransformation
    {
        private readonly IMemoryCache _cache;
        private readonly UserService _svc;

        public UserClaimTransformation(
            IMemoryCache cache,
            UserService svc
        )
        {
            _cache = cache;
            _svc = svc;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            string subject = principal.Subject()
                ?? throw new ArgumentException("ClaimsPrincipal requires 'sub' claim");

            if (! _cache.TryGetValue<User>(subject, out User user))
            {
                user = await _svc.Retrieve(subject) ?? new User
                {
                    Id = subject,
                    Name = principal.Name()
                };

                // TODO: implement IChangeToken for this

                _cache.Set<User>(subject, user, new TimeSpan(0, 5, 0));
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(AppConstants.SubjectClaimName, user.Id));
            claims.Add(new Claim(AppConstants.NameClaimName, user.Name ?? ""));
            claims.Add(new Claim(AppConstants.RoleListClaimName, user.Role.ToString()));

            foreach(string role in user.Role.ToString().Replace(" ", "").Split(','))
                claims.Add(new Claim(AppConstants.RoleClaimName, role));

            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
                    principal.Identity.AuthenticationType,
                    AppConstants.NameClaimName,
                    AppConstants.RoleClaimName
                )
            );
        }
    }
}
