// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Data;

public class ApiKey : IEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset GeneratedOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public required string Key { get; set; }

    // relational properties
    public required string OwnerId { get; set; }
    public User Owner { get; set; }
}
