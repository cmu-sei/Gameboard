using Gameboard.Api.Structure;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class AssertionExtensions
{
    public static async Task<bool> IsEmpty(this HttpContent content)
    {
        var bytesContent = await content.ReadAsByteArrayAsync();
        return bytesContent.Length == 0;
    }

    public static async Task ShouldYieldGameboardValidationException<T>(this Task<HttpResponseMessage> request) where T : GameboardValidationException
    {
        var response = await request;
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!IsExceptionString<T>(responseContent))
            throw new WrongExceptionType(typeof(T), responseContent);
    }

    private static bool IsExceptionString<T>(string responseContent) where T : GameboardValidationException
        => responseContent.Contains(typeof(T).ToExceptionCode());
}
