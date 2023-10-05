using Gameboard.Api.Features.Challenges;

namespace Gameboard.Api.Tests.Unit;

public class ChallengeDocsServiceTests
{
    [Fact]
    public void ReplaceRelativeUris_WithRelativeUrl_ResolvesChallengeDocsEndpoint()
    {
        // arrange
        var coreOptions = new CoreOptions { ChallengeDocUrl = "https://google.com" };
        var sut = new ChallengeDocsService(coreOptions);

        // act
        var result = sut.ReplaceRelativeUris("[a link](docs/1234.jpg)");

        // assert
        result.ShouldBe("[a link](https://google.com/docs/1234.jpg)");
    }

    [Fact]
    public void ReplaceRelativeUris_WithAbsoluteUrl_ReturnsUnmodified()
    {
        // arrange
        var coreOptions = new CoreOptions { ChallengeDocUrl = "https://google.com" };
        var sut = new ChallengeDocsService(coreOptions);

        // act
        var result = sut.ReplaceRelativeUris("[a link](https://another.domain/docs/1234.jpg)");

        // assert
        result.ShouldBe("[a link](https://another.domain/docs/1234.jpg)");
    }
}
