using Gameboard.Api.Common.Services;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Tests.Unit;

public class AppUrlServiceTests
{
    [Fact]
    public void GetBaseUrl_WithFixedRequest_BuildsExpected()
    {
        // given
        var httpContext = A.Fake<HttpContext>();
        httpContext.Request.Host = new HostString("gameboard.com", 4202);
        httpContext.Request.Scheme = "https";
        httpContext.Request.PathBase = "/test/gb";

        var httpContextAccessor = A.Fake<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = httpContext;

        // (sut)
        var sut = new AppUrlService(httpContextAccessor);

        // when
        var result = sut.GetBaseUrl();

        // then
        result.ShouldBe("https://gameboard.com:4202/test/gb");
    }

    [Fact]
    public void GetAbsoluteUrlFromRelative_WithFixedRequest_BuildsExpected()
    {
        // given
        var httpContext = A.Fake<HttpContext>();
        httpContext.Request.Host = new HostString("gameboard.com", 4202);
        httpContext.Request.Scheme = "https";
        httpContext.Request.PathBase = "/test/gb";

        var httpContextAccessor = A.Fake<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = httpContext;

        // (sut)
        var sut = new AppUrlService(httpContextAccessor);

        // when
        var result = sut.ToAppAbsoluteUrl("mks");

        // then
        result.ShouldBe("https://gameboard.com:4202/test/gb/mks");
    }
}
