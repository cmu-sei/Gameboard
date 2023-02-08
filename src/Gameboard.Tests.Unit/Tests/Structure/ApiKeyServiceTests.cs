using Gameboard.Api;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace Gameboard.Tests.Unit;

public class ApiKeyServiceTests
{
    private ApiKeyService GetSut(AppSettings settings, IRandomService? random = null) => new ApiKeyService
        (
            settings,
            A.Fake<IPasswordHasher<Api.Data.User>>(),
            random ?? A.Fake<IRandomService>(),
            A.Fake<IApiKeyStore>()
        );

    [Theory, GameboardAutoData]
    public void GeneratePlainKey_WithPrefixSetting_RespectsPrefix(string prefix)
    {
        // arrange
        var sut = GetSut(new AppSettings { ApiKey = new ApiKeyOptions { KeyPrefix = prefix } });

        // act
        var result = sut.GeneratePlainKey();

        // assert
        result.ShouldStartWith(prefix);
    }

    [Theory, InlineAutoData(3, 15)]
    public void GeneratePlainKey_WithPrefixAndRandomnessLength_GeneratesExpectedLength(int prefixLength, int randomnessLength, string randomness, IFixture fixture)
    {
        // arrange
        var appSettings = new AppSettings
        {
            ApiKey = new ApiKeyOptions
            {
                KeyPrefix = fixture.Create<string>().Substring(0, prefixLength),
                RandomCharactersLength = randomnessLength
            }
        };

        var random = A.Fake<IRandomService>();
        A.CallTo(() => random.GetString(A<int>.Ignored, A<int>.Ignored)).Returns(randomness);

        var sut = GetSut(appSettings, random: random);

        // act
        var result = sut.GeneratePlainKey();

        // assert
        result?.Length.ShouldBe(prefixLength + randomnessLength);
    }

    [Theory, InlineData("GB", "1234567890")]
    public void GeneratePlainKey_WithFixedValues_GeneratesExpectedKey(string prefix, string randomness)
    {
        // arrange
        var appSettings = new AppSettings
        {
            ApiKey = new ApiKeyOptions
            {
                KeyPrefix = prefix,
                RandomCharactersLength = 10
            }
        };

        var random = A.Fake<IRandomService>();
        A.CallTo(() => random.GetString(A<int>.Ignored, A<int>.Ignored)).Returns(randomness);

        var sut = GetSut(appSettings, random: random);

        // act
        var result = sut.GeneratePlainKey();

        // assert
        result.ShouldBe("GB1234567890");
    }
}