using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = TestAuthenticationUser.DEFAULT_USERID;
    public TestAuthenticationUser Actor { get; set; } = new TestAuthenticationUser();
}
