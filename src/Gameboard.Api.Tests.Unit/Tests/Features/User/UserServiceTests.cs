using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

public class UserServiceTests
{
    private UserService GetTestableSut
    (
        INowService? now = null,
        SponsorService? sponsorService = null,
        IStore<Data.User>? userStore = null,
        IMapper? mapper = null,
        IMemoryCache? cache = null,
        INameService? namesvc = null
    ) => new UserService
    (
        now ?? A.Fake<INowService>(),
        sponsorService ?? A.Fake<SponsorService>(),
        userStore ?? A.Fake<IStore<Data.User>>(),
        mapper ?? A.Fake<IMapper>(),
        cache ?? A.Fake<IMemoryCache>(),
        namesvc ?? A.Fake<INameService>()
    );

    [Theory]
    [InlineData(UserRole.Admin, UserRole.Admin)]
    [InlineData(UserRole.Admin | UserRole.Registrar, UserRole.Registrar)]
    public void HasRole_WithMatchingRole_ReturnsTrue(UserRole usersRoles, UserRole targetRole)
    {
        // given
        var sut = GetTestableSut();
        var user = new User() { Role = usersRoles };

        // when
        var result = sut.HasRole(user, targetRole);

        // then
        result.ShouldBeTrue();

    }

    [Theory]
    [InlineData(UserRole.Admin, UserRole.Registrar)]
    [InlineData(UserRole.Registrar | UserRole.Support, UserRole.Admin)]
    public void HasRole_WithoutMatchingRole_ReturnsFalse(UserRole usersRoles, UserRole targetRole)
    {
        // given
        var sut = GetTestableSut();
        var user = new User() { Role = usersRoles };

        // when
        var result = sut.HasRole(user, targetRole);

        // then
        result.ShouldBeFalse();
    }
}
