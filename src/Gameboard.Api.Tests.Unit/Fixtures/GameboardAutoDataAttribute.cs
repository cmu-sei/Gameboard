// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Tests.Shared.Fixtures;

namespace Gameboard.Api.Tests.Unit.Fixtures;

public class GameboardAutoDataAttribute : AutoDataAttribute
{
    private static readonly IFixture FIXTURE = new Fixture()
        .Customize(new AutoFakeItEasyCustomization())
        .Customize(new GameboardCustomization());

    public GameboardAutoDataAttribute() : base(() => FIXTURE) { }
}
