using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameTeamService
{
    Task CreateTeams(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task DeleteTeamExternalData(CancellationToken cancellationToken, params string[] teamIds);

    /// <summary>
    /// Returns the metadata for a given team in an external game. Note that this function will return the
    /// "Not Started" status for teams which have never played the game and thus have no metadata. No error
    /// codes are given in this case.
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ExternalGameTeam> GetTeam(string teamId, CancellationToken cancellationToken);
    Task UpdateGameDeployStatus(string gameId, ExternalGameTeamDeployStatus status, CancellationToken cancellationToken);
    Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameTeamDeployStatus status, CancellationToken cancellationToken);
    Task UpdateTeamExternalUrl(string teamId, string url, CancellationToken cancellationToken);
}

internal class ExternalGameTeamService : IExternalGameTeamService
{
    private readonly IGuidService _guids;
    private readonly IStore _store;

    public ExternalGameTeamService(IGuidService guids, IStore store)
    {
        _guids = guids;
        _store = store;
    }

    public async Task CreateTeams(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        // first, delete any metadata associated with a previous attempt
        await DeleteTeamExternalData(cancellationToken, teamIds.ToArray());

        // then create an entry for each team in this game
        await _store.SaveAddRange(teamIds.Select(teamId => new ExternalGameTeam
        {
            Id = _guids.GetGuid(),
            GameId = gameId,
            TeamId = teamId,
            DeployStatus = ExternalGameTeamDeployStatus.NotStarted
        }).ToArray());
    }

    public async Task DeleteTeamExternalData(CancellationToken cancellationToken, params string[] teamIds)
        => await _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => teamIds.Contains(t.TeamId))
            .ExecuteDeleteAsync(cancellationToken);

    public Task<ExternalGameTeam> GetTeam(string teamId, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .SingleOrDefaultAsync(r => r.TeamId == teamId, cancellationToken);

    public async Task UpdateGameDeployStatus(string gameId, ExternalGameTeamDeployStatus status, CancellationToken cancellationToken)
    {
        await _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(d => d.GameId == gameId)
            .ExecuteUpdateAsync(up => up.SetProperty(d => d.DeployStatus, status));
    }

    public Task UpdateTeamDeployStatus(IEnumerable<string> teamIds, ExternalGameTeamDeployStatus status, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => teamIds.Contains(t.TeamId))
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.DeployStatus, status), cancellationToken);


    public async Task UpdateTeamExternalUrl(string teamId, string url, CancellationToken cancellationToken)
    {
        await _store
            .WithNoTracking<ExternalGameTeam>()
            .Where(t => t.TeamId == teamId)
            .ExecuteUpdateAsync(up => up.SetProperty(t => t.ExternalGameUrl, url), cancellationToken);
    }
}
