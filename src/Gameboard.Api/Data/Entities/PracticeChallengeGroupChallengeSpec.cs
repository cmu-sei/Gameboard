namespace Gameboard.Api.Data;

public sealed class PracticeChallengeGroupChallengeSpec : IEntity
{
    public string Id { get; set; }
    public required string PracticeChallengeGroupId { get; set; }
    public PracticeChallengeGroup PracticeChallengeGroup { get; set; }
    public required string ChallengeSpecId { get; set; }
    public Data.ChallengeSpec ChallengeSpec { get; set; }
}
