// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Data;

public sealed class PracticeChallengeGroupChallengeSpec : IEntity
{
    public string Id { get; set; }
    public required string PracticeChallengeGroupId { get; set; }
    public PracticeChallengeGroup PracticeChallengeGroup { get; set; }
    public required string ChallengeSpecId { get; set; }
    public Data.ChallengeSpec ChallengeSpec { get; set; }
}
