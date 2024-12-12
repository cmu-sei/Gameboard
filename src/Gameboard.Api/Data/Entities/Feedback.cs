// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Data
{
    public class Feedback : IEntity
    {
        public string Id { get; set; } // id for unique submission
        public string UserId { get; set; } // user id of player submitting response
        public string PlayerId { get; set; } // individual's player id for board
        public string GameId { get; set; } // always a game specified, even for challenge feedback
        public string ChallengeId { get; set; } // if NULL then game-level feedback
        public string ChallengeSpecId { get; set; } // consistent across teams playing same challenge
        public string Answers { get; set; } // JSON dump of questions and answers
        public bool Submitted { get; set; } // True when officially submitted, as opposed to autosaved/in progress
        public DateTimeOffset Timestamp { get; set; } // time last saved
        public User User { get; set; }
        public Player Player { get; set; }
        public Game Game { get; set; }
        public Challenge Challenge { get; set; }
        public ChallengeSpec ChallengeSpec { get; set; }
    }
}
