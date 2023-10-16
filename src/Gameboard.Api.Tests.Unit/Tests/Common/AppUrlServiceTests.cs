using Gameboard.Api.Common.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api.Tests.Unit;

public class AppUrlServiceTests
{
    [Fact]
    public void GetBaseUrl_WithFixedRequest_BuildsExpected()
    {
        // given
        var httpContext = A.Fake<HttpContext>();
        httpContext.Request.Host = new HostString("gameboard.com");
        httpContext.Request.Scheme = "https";
        httpContext.Request.PathBase = "/test/gb";

        var httpContextAccessor = A.Fake<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = httpContext;

        // (sut)
        var sut = new AppUrlService(A.Fake<IWebHostEnvironment>(), httpContextAccessor);

        // when
        var result = sut.GetBaseUrl();

        // then
        result.ShouldBe("https://gameboard.com/test/gb");
    }

    [Fact]
    public void GetAbsoluteUrlFromRelative_WithFixedRequest_BuildsExpected()
    {
        // given
        var httpContext = A.Fake<HttpContext>();
        httpContext.Request.Host = new HostString("gameboard.com");
        httpContext.Request.Scheme = "https";
        httpContext.Request.PathBase = "/test/gb";

        var httpContextAccessor = A.Fake<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = httpContext;

        // (sut)
        var sut = new AppUrlService(A.Fake<IWebHostEnvironment>(), httpContextAccessor);

        // when
        var result = sut.ToAppAbsoluteUrl("mks");

        // then
        result.ShouldBe("https://gameboard.com/test/gb/mks");
    }
}
