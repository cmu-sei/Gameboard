using System.Text.Encodings.Web;
using Gameboard.Api;
using Gameboard.Api.Auth;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Gameboard.Api.Tests.Unit;

public class ApiKeyAuthenticationHandlerTests
{
    // TODO: constructor fixture thing
    private ApiKeyAuthenticationHandler GetSut() => new ApiKeyAuthenticationHandler
    (
            A.Fake<IOptionsMonitor<ApiKeyAuthenticationOptions>>(),
            A.Fake<ILoggerFactory>(),
            A.Fake<UrlEncoder>(),
            A.Fake<ISystemClock>(),
            A.Fake<IApiKeysService>()
    );

    [Theory, GameboardAutoData]
    public void ResolveRequestApiKey_WithXApiKeyHeader_ResolvesApiKey(string apiKey)
    {
        // arrange
        var sut = GetSut();

        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers).Returns(new HeaderDictionary
        {
            new KeyValuePair<string, StringValues>(ApiKeyAuthentication.ApiKeyHeaderName, new StringValues(apiKey))
        });

        // act 
        var result = sut.ResolveRequestApiKey(request);

        // assert
        result.ShouldBe(apiKey);
    }

    [Theory, GameboardAutoData]
    public void ResolveRequestApiKey_WithAuthorizationHeader_DoesntResolveApiKey(string apiKey)
    {
        // arrange
        var sut = GetSut();

        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers).Returns(new HeaderDictionary
        {
            new KeyValuePair<string, StringValues>(ApiKeyAuthentication.AuthorizationHeaderName, $"{ApiKeyAuthentication.AuthenticationScheme} {apiKey}")
        });

        // act 
        var result = sut.ResolveRequestApiKey(request);

        // assert
        result.ShouldBeNull();
    }
}
