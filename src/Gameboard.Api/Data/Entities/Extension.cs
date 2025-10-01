// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Data;

public enum ExtensionType
{
    Mattermost
}

public class Extension : IEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ExtensionType Type { get; set; }
    public string HostUrl { get; set; }
    public string Token { get; set; }
    public bool IsEnabled { get; set; }
}
