using Gameboard.Api.Data;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceTests
{
    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScoreZero_ReturnsEmptyArray(IFixture fixture)
    {
        // arrange
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IStore>();
        var fakePlayers = new Data.Player[]
        {
            new Data.Player
            {
                PartialCount = 1,
                Game = new Data.Game
                {
                    CertificateTemplate = fixture.Create<string>(),
                    GameEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(1)
                },
                Score = 0,
                SessionEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(2),
                User = new Api.Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        A.CallTo(() => fakeStore.WithNoTracking<Data.Player>()).Returns(fakePlayers);

        var sut = PlayerServiceTestHelpers.GetTestableSut(store: fakeStore);

        // act
        var result = await sut.MakeCertificates(userId);

        // assert
        result.ShouldBe(Array.Empty<PlayerCertificate>());
    }

    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScore1_ReturnsOneCertificate(IFixture fixture)
    {
        // arrange
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IStore>();
        var fakePlayers = new Data.Player[]
        {
            new()
            {
                PartialCount = 0,
                Game = new Data.Game
                {
                    CertificateTemplate = fixture.Create<string>(),
                    GameEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(1)
                },
                Score = 1,
                SessionEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(2),
                UserId = userId,
                User = new Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        A.CallTo(() => fakeStore.WithNoTracking<Data.Player>()).Returns(fakePlayers);

        var sut = PlayerServiceTestHelpers.GetTestableSut(store: fakeStore);

        // act
        var result = await sut.MakeCertificates(userId);

        // assert
        result.Count().ShouldBe(1);
    }
}
