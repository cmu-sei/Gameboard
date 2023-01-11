using AutoMapper;
using FakeItEasy;
using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Tests.Unit;

public class UserServiceTests
{
    // [Theory, GameboardAutoData]
    // public async Task Create_WhenIsntInStore_SetsNameProperties(IFixture fixture)
    // {
    //     // given
    //     var userId = fixture.Create<string>();
    //     var name = fixture.Create<string>();

    //     var nameSvc = A.Fake<INameService>();
    //     A.CallTo(() => nameSvc.GetRandomName()).Returns(name);

    //     var store = A.Fake<IUserStore>();
    //     A.CallTo(() => store.Retrieve(userId)).Returns(null as Api.Data.User);

    //     var sut = fixture.Build<UserService>()
    //         .FromFactory<INameService>
    //         (
    //             nameSvc =>
    //             new UserService
    //             (
    //                 store,
    //                 A.Fake<IMapper>(),
    //                 A.Fake<IMemoryCache>(),
    //                 nameSvc,
    //                 A.Fake<Defaults>()
    //             )
    //         ).Create();

    //     // when
    //     var result = await sut.Create(new Api.NewUser
    //     {
    //         Id = fixture.Create<string>(),
    //         Name = "this will be overwritten",
    //         Sponsor = fixture.Create<string>()
    //     });

    //     // then
    //     result.ShouldNotBeNull();
    //     result.ApprovedName.ShouldNotBeNullOrWhiteSpace();
    //     result.Name.ShouldNotBeNullOrWhiteSpace();
    // }
}