using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Services;

namespace Gameboard.Tests.Unit;

public class ApiKeyServiceTests
{
    private ApiKeysService GetSut(ApiKeyOptions options, IRandomService? random = null) => new ApiKeysService
        (
            options,
            A.Fake<IGuidService>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            A.Fake<IHashService>(),
            random ?? A.Fake<IRandomService>(),
            A.Fake<IApiKeysStore>(),
            A.Fake<IUserStore>()
        );

    [Theory, InlineAutoData(3, 15)]
    public void GeneratePlainKey_WithPrefixAndRandomnessLength_GeneratesExpectedLength(int randomnessLength, string randomness, IFixture fixture)
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
        var result = sut.GeneratePlainKey();

        // assert
        result?.Length.ShouldBe(randomnessLength);
    }

    [Theory, InlineData("1234567890")]
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
        var result = sut.GeneratePlainKey();

        // assert
        result.ShouldBe("GB1234567890");
    }
}
