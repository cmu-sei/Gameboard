using Gameboard.Api;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthenticationUser
{
    public static string DEFAULT_USERID = "UserId-IntegrationTester";

    public string Id { get; set; } = DEFAULT_USERID;
    public string Name { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Member;
}