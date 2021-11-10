namespace Gameboard.Api
{
    public class ChallengeGate
    {
        public string Id { get; set; }
        public string GameId { get; set; }
        public string TargetId { get; set; }
        public string RequiredId { get; set; }
        public double RequiredScore { get; set; }
    }

    public class NewChallengeGate
    {
        public string GameId { get; set; }
        public string TargetId { get; set; }
        public string RequiredId { get; set; }
        public double RequiredScore { get; set; }
    }

    public class ChangedChallengeGate
    {
        public string Id { get; set; }
        public string TargetId { get; set; }
        public string RequiredId { get; set; }
        public double RequiredScore { get; set; }
    }
}
