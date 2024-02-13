using System.Threading.Tasks;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Hubs;

public class ScoreHubEvent<TData> where TData : class
{
    public required string GameId { get; set; }
    public required ScoreHubEventType EventType { get; set; }
    public required TData Data { get; set; }
}

public enum ScoreHubEventType
{
    ScoreUpdated
}

public sealed class ScoreUpdatedEvent
{
    public required GameScore Score { get; set; }
    public ScoreUpdatedTeamChangeSummary TeamChangeSummary { get; set; }
}

public sealed class ScoreUpdatedTeamChangeSummary
{
    public required string TeamId { get; set; }
    public required string TeamName { get; set; }
    public required double PreviousScore { get; set; }
    public required double CurrentScore { get; set; }
    public required int PreviousRank { get; set; }
    public required int CurrentRank { get; set; }
}

public interface IScoreHubEvent
{
    Task SendScoreUpdated(ScoreHubEvent<ScoreUpdatedEvent> ev);
}
