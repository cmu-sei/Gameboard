// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Unit.Fixtures;

public class GameboardApiTestsFakingException : Exception
{
    public GameboardApiTestsFakingException(string message) : base(message) { }
}
