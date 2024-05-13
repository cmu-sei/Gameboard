using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface IStandardGameModeService : IGameModeService { }

internal class StandardGameModeService : IStandardGameModeService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public StandardGameModeService(INowService now, IStore store)
    {
        _now = now;
        _store = store;
    }

    public bool DeployResourcesOnSessionStart => false;

    public bool RequireSynchronizedSessions => false;

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.ArchiveChallenges;

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        return Task.FromResult(GamePlayState.NotStarted);
    }

    public async Task<GamePlayState> GetGamePlayStateForTeam(string teamId, CancellationToken cancellationToken)
    {
        var teamSession = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .Select(p => new
            {
                p.SessionBegin,
                p.SessionEnd,
                p.Role
            })
            .ToArrayAsync(cancellationToken);

        var begin = teamSession.Select(p => p.SessionBegin).Distinct().Single();
        var end = teamSession.Select(p => p.SessionEnd).Distinct().Single();

        if (begin.IsEmpty())
            return GamePlayState.NotStarted;

        var nowish = _now.Get();
        if (begin <= nowish && (end.IsEmpty() || end >= nowish))
            return GamePlayState.Started;

        return GamePlayState.GameOver;
    }

    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
