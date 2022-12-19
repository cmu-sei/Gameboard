using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Tests.Integration.Fixtures;

internal class TestAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public const string AuthenticationSchemeName = "Test";
    public AuthenticationScheme AuthScheme { get; } = new AuthenticationScheme(name: AuthenticationSchemeName, displayName: AuthenticationSchemeName, handlerType: typeof(TestAuthenticationHandler));
    public string DefaultUserId { get; set; } = null!;
}
