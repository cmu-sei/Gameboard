// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Data
{
    public class DbSeedModel
    {
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Game> Games { get; set; } = new List<Game>();
        public ICollection<ChallengeSpec> ChallengeSpecs { get; set; } = new List<ChallengeSpec>();
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
        public ICollection<Sponsor> Sponsors { get; set; } = new List<Sponsor>();
        public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
    }
}
