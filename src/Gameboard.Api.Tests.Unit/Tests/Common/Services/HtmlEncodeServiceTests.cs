using Gameboard.Api.Common;

namespace Gameboard.Api.Tests.Unit;

public class HtmlEncodeServiceTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("""<a href="https://google.com"><font size=100 color=red>You must click me</font></a>""")]
    [InlineData("""<script>alert("1")</script> """)]
    public void Encode_WithAttackHtml_Encodes(string attackHtml)
    {
        // given
        var service = new HtmlEncodeService();

        // when
        var result = service.Encode(attackHtml);

        // then
        result.ShouldNotContain("<");
        result.ShouldNotContain(">");
    }
}
