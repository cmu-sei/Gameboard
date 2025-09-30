// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class GameBuilder
{
    public Action<Game>? Configure { get; set; }
    public bool WithChallengeSpec { get; set; } = false;
    public Action<ChallengeSpec>? ConfigureChallengeSpec { get; set; }

    public static GameBuilder WithConfig(Action<Game> configure)
    {
        return new GameBuilder { Configure = configure };
    }
}
