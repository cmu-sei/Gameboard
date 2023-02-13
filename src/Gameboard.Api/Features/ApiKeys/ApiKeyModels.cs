using System;
using System.Text.Json.Serialization;

namespace Gameboard.Api.Features.ApiKeys;

public class NewApiKey
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
}

public class ApiKeyHash
{
    public string UserApiKey { get; set; }

    // ↓ this is just to ensure we never send this down to a client application
    [JsonIgnore]
    public string HashedApiKey { get; set; }
}

public class CreateApiKeyResult
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset GeneratedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public string UnhashedKey { get; set; }
    public string OwnerId { get; set; }
}
