// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{
    public class ChallengeEvent
    {
        public string Id { get; set; }
        public string ChallengeId { get; set; }
        public string UserId { get; set; }
        public string TeamId { get; set; }
        public string Text { get; set; }
        public ChallengeEventType Type { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public class NewChallengeEvent
    {
        public string Name { get; set; }
    }

    public class ChangedChallengeEvent
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

}
