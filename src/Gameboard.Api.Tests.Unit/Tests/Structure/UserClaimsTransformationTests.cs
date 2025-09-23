using System.Security.Claims;
using AutoMapper;
using Castle.Core.Logging;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

public class UserClaimsTransformationTests
{
    [Theory, GameboardAutoData]
    public async Task TransformAsync_WithCacheMiss_SetsInferredClaims(string sponsorId, string userId)
    {
        // arrange
        var cache = A.Fake<IMemoryCache>();
        var logger = A.Fake<ILogger<UserClaimTransformation>>();

        var mapper = A.Fake<IMapper>();
        A.CallTo(() => mapper.Map<Api.User?>(null))
            .WithAnyArguments()
            .Returns(null);

        var sponsorService = A.Fake<ISponsorService>();
        A.CallTo(() => sponsorService.GetDefaultSponsor())
            .WithAnyArguments()
            .Returns(new Data.Sponsor { Id = sponsorId });

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.User?>())
            .Returns(Array.Empty<Data.User>().BuildMock());

        var sut = new UserClaimTransformation(logger, cache, mapper, new OidcOptions(), sponsorService, store, A.Fake<IUserRolePermissionsService>());
        var claimsPrincipal = new ClaimsPrincipal();
        claimsPrincipal.AddIdentity(new ClaimsIdentity(new Claim(AppConstants.SubjectClaimName, userId).ToEnumerable()));

        // when
        var result = await sut.TransformAsync(claimsPrincipal);

        // then
        result.Claims.Any(c => c.Type == AppConstants.SubjectClaimName).ShouldBeTrue();
        result.Claims.Any(c => c.Type == AppConstants.NameClaimName).ShouldBeTrue();
        result.Claims.Any(c => c.Type == AppConstants.ApprovedNameClaimName).ShouldBeTrue();
        result.Claims.Single(c => c.Type == AppConstants.RoleClaimName).Value.ShouldBe(UserRoleKey.Member.ToString());
        result.Claims.Single(c => c.Type == AppConstants.SponsorClaimName).Value.ShouldBe(sponsorId);
    }
}
