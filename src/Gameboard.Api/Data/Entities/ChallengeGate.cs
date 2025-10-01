// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Data
{
    public class ChallengeGate: IEntity
    {
        public string Id { get; set; }
        public string GameId { get; set; }
        public string TargetId { get; set; }
        public string RequiredId { get; set; }
        public double RequiredScore { get; set; }
        public Game Game { get; set; }
    }
}
