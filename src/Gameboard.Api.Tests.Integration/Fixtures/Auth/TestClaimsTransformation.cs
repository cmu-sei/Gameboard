using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Api.Tests.Integration.Fixtures
{
    internal class TestClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }
}
