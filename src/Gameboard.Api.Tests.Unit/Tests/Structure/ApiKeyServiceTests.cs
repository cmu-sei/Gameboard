using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class ApiKeyServiceTests
{
    private ApiKeysService GetSut(ApiKeyOptions options, IRandomService? random = null) => new ApiKeysService
        (
            options,
            A.Fake<IGuidService>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            random ?? A.Fake<IRandomService>(),
            A.Fake<IApiKeysStore>(),
            A.Fake<IStore<Data.User>>()
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
}
