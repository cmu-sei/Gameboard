// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Hubs;

public interface ICanonicalGroupIdProvider
{
    GameboardHubType GroupType { get; }
}

internal class CanonicalGroupIdProvider
{
    public string GetCanonicalGroupId(GameboardHubType groupType, string groupIdentifier)
        => $"{groupType.ToString().ToLower()}-{groupIdentifier}";
}
