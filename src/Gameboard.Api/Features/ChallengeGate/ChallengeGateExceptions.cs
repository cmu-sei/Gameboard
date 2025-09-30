// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;

internal class CyclicalGateConfiguration : GameboardException
{
    public CyclicalGateConfiguration(string targetId, string requiredId, string cycleDesc)
        : base($"Challenge spec {requiredId} can't be used as a prerequisite for challenge spec {targetId} because they have cyclical dependency ({cycleDesc}).") { }
}
