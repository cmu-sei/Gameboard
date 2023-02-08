using System;

namespace Gameboard.Api.Data;

public class ApiKey : IEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset GeneratedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public required string Key { get; set; }

    // relational properties
    public required string OwnerId { get; set; }
    public required User ApiKeyOwner { get; set; }
}
