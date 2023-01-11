using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Tests.Integration.Fixtures;

internal class TestAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = null!;
}
