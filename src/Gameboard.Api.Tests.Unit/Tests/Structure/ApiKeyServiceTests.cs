using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class ApiKeyServiceTests
{
    private ApiKeysService GetSut(ApiKeyOptions? options = null, IRandomService? random = null, IStore<Data.User>? userStore = null) => new
    (
        options ?? new ApiKeyOptions { RandomCharactersLength = 15 },
        A.Fake<IGuidService>(),
        A.Fake<IMapper>(),
        A.Fake<INowService>(),
        random ?? A.Fake<IRandomService>(),
        A.Fake<IApiKeysStore>(),
        userStore ?? A.Fake<IStore<Data.User>>()
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
        var userStore = A.Fake<IStore<Data.User>>();
        var fakeUsers = new Data.User[]
        {
            new ()
            {
                ApiKeys = new Data.ApiKey[]
                {
                    new ()
                    {
                        Id =  fixture.Create<string>(),
                        Name = fixture.Create<string>(),
                        Key = apiKey.ToSha256(),
                        GeneratedOn = fixture.Create<DateTimeOffset>(),
                        OwnerId = fixture.Create<string>()
                    }
                },
                Enrollments = Array.Empty<Data.Player>()
            }
        }.BuildMock();

        A.CallTo(() => userStore.ListWithNoTracking()).Returns(fakeUsers);
        var sut = GetSut(userStore: userStore);

        // when
        var result = sut.GetUserFromApiKey(apiKey);

        // then
        result.ShouldNotBeNull();
    }
}
