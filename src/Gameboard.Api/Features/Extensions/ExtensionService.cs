using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Extensions;

public interface IExtensionService
{
    Task NotifyScored(ExtensionMessage message);
}

internal class MattermostExtensionService : IExtensionService
{
    private readonly Extension _extension;
    private readonly IHttpClientFactory _httpClientFactory;

    public MattermostExtensionService
    (
        Extension extension,
        IHttpClientFactory httpClientFactory
    )
    {
        _extension = extension;
        _httpClientFactory = httpClientFactory;
    }

    public async Task NotifyScored(ExtensionMessage message)
    {
        var client = BuildClient();
        var attachments = message.TextAttachments.Select(a => new { pretext = a.Key, text = a.Value }).ToArray();
        var body = new
        {
            channel_id = "bf3xfr4cjjf3jyq3hoqa7ub1ro",
            message = message.Text,
            props = new
            {
                attachments
            }
        };

        await client.PostAsync
        (
            "/posts",
            JsonContent.Create(body)
        );
    }

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_extension.Token}");
        client.BaseAddress = new Uri(_extension.HostUrl);
        return client;
    }
}
