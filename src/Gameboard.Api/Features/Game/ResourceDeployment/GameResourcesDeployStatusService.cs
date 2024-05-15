using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games;

public interface IGameResourcesDeployStatusService
{
    Task<GameResourcesDeployStatus> GetStatus(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken);
}

internal class GameResourcesDeployStatusService : IGameResourcesDeployStatusService
{
    private readonly IJsonService _json;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GameResourcesDeployStatusService
    (
        IJsonService json,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        _json = json;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<GameResourcesDeployStatus> GetStatus(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        // we use a factory scope for this rather than relying on IStore because this can be requested from multiple threads at once
        // (even during the same request/background task)
        using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameboardDbContext>();

        var challenges = await dbContext
            .Challenges
            .AsNoTracking()
            .Where(c => c.GameId == gameId)
            .Where(c => teamIds.Contains(c.TeamId))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.GameEngineType,
                c.HasDeployedGamespace,
                c.Points,
                c.Score,
                c.SpecId,
                c.State,
                c.TeamId
            })
            .ToArrayAsync(cancellationToken);

        var players = await dbContext
            .Players
            .AsNoTracking()
            .Where(p => teamIds.Contains(p.TeamId))
            .Where(p => p.GameId == gameId)
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.GameId,
                p.Role,
                p.TeamId,
                p.UserId
            })
            .ToArrayAsync(cancellationToken);

        var game = await dbContext
            .Games
            .AsNoTracking()
            .Include(g => g.Specs)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Specs
            })
            .SingleAsync(g => g.Id == gameId, cancellationToken);

        return new GameResourcesDeployStatus
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            ChallengeSpecs = game.Specs.Select(s => new SimpleEntity { Id = s.Id, Name = s.Name }),
            Challenges = challenges.Select(c => new GameResourcesDeployChallenge
            {
                Id = c.Id,
                Name = c.Name,
                Engine = c.GameEngineType,
                HasGamespace = c.HasDeployedGamespace,
                IsFullySolved = c.Score >= c.Points,
                SpecId = c.SpecId,
                State = _json.Deserialize<GameEngineGameState>(c.State),
                TeamId = c.TeamId
            }),
            Teams = players
                .GroupBy(p => p.TeamId)
                .Select(gr =>
                {
                    var teamPlayers = gr.Select(p => new
                    {
                        p.Id,
                        Name = p.ApprovedName,
                        IsCaptain = p.Role == PlayerRole.Manager,
                        p.UserId
                    });

                    var captain = teamPlayers.Single(p => p.IsCaptain);

                    return new GameResourcesDeployTeam
                    {
                        Id = gr.Key,
                        Name = captain.Name,
                        Captain = new GameResourcesDeployPlayer
                        {
                            Id = captain.Id,
                            Name = captain.Name,
                            UserId = captain.UserId
                        },
                        Players = teamPlayers.Select(p => new GameResourcesDeployPlayer
                        {
                            Id = p.Id,
                            Name = p.Name,
                            UserId = p.UserId
                        })
                    };
                }),
            FailedGamespaceDeployChallengeIds = challenges
                .Where(c => !c.HasDeployedGamespace && c.Points < c.Score)
                .Select(c => c.Id)
                .ToArray()
        };
    }
}
