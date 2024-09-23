using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TestAuthenticationUser
{
    public static string DEFAULT_USERID = "UserId-IntegrationTester";

    public string Id { get; set; } = DEFAULT_USERID;
    public string Name { get; set; } = "";
    public UserRoleKey Role { get; set; } = UserRoleKey.Member;
    public string SponsorId { get; set; } = string.Empty;
}
