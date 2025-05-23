using Gameboard.Api.Structure;

namespace Gameboard.Api.Tests.Integration;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> SendDeleteWithJsonContent<TContent>(this HttpClient http, string uri, TContent body) where TContent : class
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, uri) { Content = body.ToJsonBody() };
        requestMessage.Headers.Add(System.Net.HttpRequestHeader.ContentType.ToString(), MimeTypes.ApplicationJson);

        return await http.SendAsync(requestMessage);
    }
}
