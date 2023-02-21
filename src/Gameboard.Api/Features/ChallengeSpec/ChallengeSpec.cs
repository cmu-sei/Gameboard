// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api
{
    public class ExternalSpec
    {
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public GameEngineType GameEngineType { get; set; }
    }

    public class SpecDetail : ExternalSpec
    {
        public string Tag { get; set; }
        public bool Disabled { get; set; }
        public int AverageDeploySeconds { get; set; }
        public int Points { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float R { get; set; }
    }

    public class ChallengeSpec : SpecDetail
    {
        public string Id { get; set; }
        public string GameId { get; set; }
    }

    public class NewChallengeSpec : SpecDetail
    {
        public string GameId { get; set; }
    }

    public class ChangedChallengeSpec : SpecDetail
    {
        public string Id { get; set; }

    }

    public class BoardSpec
    {
        public string Id { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Disabled { get; set; }
        public int AverageDeploySeconds { get; set; }
        public int Points { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float R { get; set; }
    }


}
