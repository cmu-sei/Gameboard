using System.Threading.Tasks;

namespace Gameboard.Api.Features.CubespaceScoreboard;

public interface ICubespaceScoreboardService
{
    Task<CubespaceScoreboardState> GetScoreboard(CubespaceScoreboardRequestPayload payload);
    void InvalidateScoreboardCache();
}