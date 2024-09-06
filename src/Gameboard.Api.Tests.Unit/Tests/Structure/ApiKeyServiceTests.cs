using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.ApiKeys;

namespace Gameboard.Api.Tests.Unit;

public class ApiKeyServiceTests
{
    private ApiKeysService GetSut(ApiKeyOptions? options = null, IRandomService? random = null, IStore? store = null) => new
    (
        options ?? new ApiKeyOptions { RandomCharactersLength = 15 },
        A.Fake<IGuidService>(),
        A.Fake<IMapper>(),
        A.Fake<INowService>(),
        random ?? A.Fake<IRandomService>(),
        store ?? A.Fake<IStore>()
    );

    [Theory, InlineAutoData(3)]
    public void GeneratePlainKey_WithRandomnessLength_GeneratesExpectedLength(int randomnessLength, string randomness)
    {
        // arrange
        var options = new ApiKeyOptions
        {
            RandomCharactersLength = randomnessLength
        };

        var random = A.Fake<IRandomService>();
        A.CallTo(() => random.GetString(A<int>.Ignored, A<int>.Ignored)).Returns(randomness);

        var sut = GetSut(options, random: random);

        // act
        var result = sut.GenerateKey();

        // assert
        result?.Length.ShouldBe(randomnessLength);
    }

    [Theory, InlineData("12345678900987654321")]
    public void GeneratePlainKey_WithFixedValues_GeneratesExpectedKey(string randomness)
    {
        // arrange
        var apiOptions = new ApiKeyOptions
        {
            RandomCharactersLength = 10
        };

        var random = A.Fake<IRandomService>();
        A.CallTo(() => random.GetString(A<int>.Ignored, A<int>.Ignored)).Returns(randomness);

        var sut = GetSut(apiOptions, random: random);

        // act
        var result = sut.GenerateKey();

        // assert
        result.ShouldBe("1234567890");
    }

    [Theory, GameboardAutoData]
    public void GetUserFromApiKey_WithUserAssignedKey_ResolvesUser(string apiKey, IFixture fixture)
    {
        // given
        var store = A.Fake<IStore>();
        var fakeUsers = new Data.User[]
        {
            new ()
            {
                ApiKeys =
                [
                    new ()
                    {
                        Id =  fixture.Create<string>(),
                        Name = fixture.Create<string>(),
                        Key = apiKey.ToSha256(),
                        GeneratedOn = fixture.Create<DateTimeOffset>(),
                        OwnerId = fixture.Create<string>()
                    }
                ],
                Enrollments = Array.Empty<Data.Player>()
            }
        }.BuildMock();

        A.CallTo(() => store.WithNoTracking<Data.User>()).Returns(fakeUsers);
        var sut = GetSut(store: store);

        // when
        var result = sut.GetUserFromApiKey(apiKey);

        // then
        result.ShouldNotBeNull();
    }
}
