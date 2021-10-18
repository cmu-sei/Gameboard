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
