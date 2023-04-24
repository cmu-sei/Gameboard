namespace Gameboard.Api.Tests.Integration;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> SendDeleteWithJsonContent<TContent>(this HttpClient http, string uri, TContent body) where TContent : class
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);
        requestMessage.Content = body.ToJsonBody();
        requestMessage.Headers.Add(System.Net.HttpRequestHeader.ContentType.ToString(), "application/json");

        return await http.SendAsync(requestMessage);
    }
}
