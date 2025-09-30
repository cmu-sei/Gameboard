// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Practice;

public sealed class CantAccessPracticeChallengeGroupsUserDataException(string userId) : GameboardValidationException($"You don't have access to challenge group data for user {userId}.") { }
