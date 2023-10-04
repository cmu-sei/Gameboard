using System;

namespace Gameboard.Api.Data
{
    public class ArchivedChallenge : IEntity
    {
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public DateTimeOffset LastScoreTime { get; set; }
        public DateTimeOffset LastSyncTime { get; set; }
        public bool HasGamespaceDeployed { get; set; }
        public PlayerMode PlayerMode { get; set; }
        public string State { get; set; }
        public int Points { get; set; }
        public int Score { get; set; }
        public long Duration { get; set; }
        public ChallengeResult Result { get; set; }
        public string Events { get; set; }
        public string Submissions { get; set; }
        public string TeamMembers { get; set; }
    }
}
