using System.Security.Claims;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

public class UserClaimsTransformationTests
{
    [Theory, GameboardAutoData]
    public async Task TransformAsync_WithCacheMiss_SetsRequiredProperties(string sponsorId, string userId)
    {
        // arrange
        var cache = A.Fake<IMemoryCache>();

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

        var sut = new UserClaimTransformation(cache, mapper, sponsorService, store);
        // var claimsPrincipal = new ClaimsPrincipal(A.Fake<IIdentity>());
        var claimsPrincipal = new ClaimsPrincipal();
        claimsPrincipal.AddIdentity(new ClaimsIdentity(new Claim(AppConstants.SubjectClaimName, userId).ToEnumerable()));

        // when
        var result = await sut.TransformAsync(claimsPrincipal);

        // then
        result.Claims.Count().ShouldBeGreaterThan(0);
    }
}
