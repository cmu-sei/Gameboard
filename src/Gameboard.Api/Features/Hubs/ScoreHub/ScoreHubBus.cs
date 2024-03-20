using System.Threading.Tasks;
using Gameboard.Api.Features.Hubs;
using Gameboard.Api.Features.Scores;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface IScoreHubBus
{
    Task SendScoreUpdated(GameScore score, ScoreUpdatedTeamChangeSummary teamChangeSummary);
}

internal class ScoreHubBus : IScoreHubBus, IGameboardHubBus
{
    private readonly IHubContext<ScoreHub, IScoreHubEvent> _hubContext;

    public ScoreHubBus(IHubContext<ScoreHub, IScoreHubEvent> hubContext)
        => _hubContext = hubContext;

    public GameboardHubType GroupType => GameboardHubType.Score;

    public async Task SendScoreUpdated(GameScore score, ScoreUpdatedTeamChangeSummary teamChangeSummary)
    {
        var evData = new ScoreUpdatedEvent
        {
            Score = score,
            TeamChangeSummary = teamChangeSummary
        };

        await _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(score.Game.Id))
            .SendScoreUpdated(new ScoreHubEvent<ScoreUpdatedEvent>
            {
                GameId = score.Game.Id,
                EventType = ScoreHubEventType.ScoreUpdated,
                Data = evData
            });
    }
}
