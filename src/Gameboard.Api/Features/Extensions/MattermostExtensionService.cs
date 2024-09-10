using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Extensions;

[DIIgnore]
internal class MattermostExtensionService(
    Extension extension,
    IHttpClientFactory httpClientFactory
    ) : IExtensionService
{
    private readonly Extension _extension = extension;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public Task NotifyScored(ExtensionMessage message)
        => PostTo("posts", message);

    public Task NotifyTicketCreated(ExtensionMessage message)
        => PostTo("posts", message);

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.SetBearerToken(_extension.Token);
        client.BaseAddress = new Uri(_extension.HostUrl);
        return client;
    }

    private async Task PostTo(string endpoint, ExtensionMessage message)
    {
        var attachments = message.TextAttachments.Select(a => new { pretext = a.Key, text = a.Value }).ToArray();
        var body = new
        {
            channel_id = "bf3xfr4cjjf3jyq3hoqa7ub1ro",
            message = message.Text,
            props = new { attachments }
        };
        var client = BuildClient();

        var response = await client.PostAsync
        (
            endpoint,
            JsonContent.Create(body)
        );

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ExtensionNotificationException(_extension.Id, _extension.Type, ex);
        }
    }
}
