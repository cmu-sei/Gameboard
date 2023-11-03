namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class AssertionExtensions
{
    public static async Task<bool> IsGameboardValidationException(this HttpContent content)
    {
        var stringContent = await content.ReadAsStringAsync();
        return stringContent.Contains("GAMEBOARD VALIDATION EXCEPTION");
    }

    public static async Task<bool> YieldsGameboardValidationException(this Task<HttpResponseMessage> request)
    {
        var response = await request;
        return await response.Content.IsGameboardValidationException();
    }

    public static async Task<bool> IsEmpty(this HttpContent content)
    {
        var bytesContent = await content.ReadAsByteArrayAsync();
        return bytesContent.Length == 0;
    }
}
