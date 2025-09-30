// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Challenges;

internal class CantGradeBecauseGameExecutionPeriodIsOver : GameboardValidationException
{
    public CantGradeBecauseGameExecutionPeriodIsOver(string challengeId, DateTimeOffset gameEnd, DateTimeOffset now)
        : base($"Can't grade challenge {challengeId} because its game execution period is over (ended at {gameEnd}, it's currently {now}).") { }
}

internal class CantStartBecauseGameExecutionPeriodIsOver : GameboardValidationException
{
    public CantStartBecauseGameExecutionPeriodIsOver(string challengeSpecId, string playerId, DateTimeOffset gameEnd, DateTimeOffset now)
        : base($"Can't start challenge spec {challengeSpecId} for player {playerId} because its game execution period is over (ended at {gameEnd}, it's currently {now}).") { }
}

internal class GamespaceLimitReached : GameboardException
{
    public GamespaceLimitReached(string gameId, string teamId) : base($""" Team(s) {teamId} are already at the maximum number of gamespaces permitted for game "{gameId}." """) { }
}

internal class GraderUrlResolutionError : GameboardException
{
    public GraderUrlResolutionError() : base("Gameboard was unable to resolve a grader URL.") { }
}
