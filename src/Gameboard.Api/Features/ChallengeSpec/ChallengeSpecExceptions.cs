// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeSpecs;

public sealed class NonExistentSupportKey : GameboardValidationException
{
    public NonExistentSupportKey(string key) : base($"""No challenge spec exists with support key "{key}".""") { }
}
