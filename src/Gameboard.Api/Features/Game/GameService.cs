// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Services;

public interface IGameService
{
    Task<Game> Create(NewGame model);
    Task Delete(string id);
    Task<string> Export(GameSpecExport model);
    Task<SyncStartState> GetSyncStartState(string gameId);
    Task HandleSyncStartStateChanged(string gameId, User actor);
    Task<Game> Import(GameSpecImport model);
    IQueryable<Data.Game> BuildQuery(GameSearchFilter model = null, bool sudo = false);
    Task<IEnumerable<Game>> List(GameSearchFilter model = null, bool sudo = false);
    Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo);
    Task ReRank(string id);
    Task<Game> Retrieve(string id, bool accessHidden = true);
    Task<ChallengeSpec[]> RetrieveChallenges(string id);
    Task<SessionForecast[]> SessionForecast(string id);
    Task<SynchronizedGameStartedState> StartSynchronizedSession(string gameId);
    Task Update(ChangedGame account);
    Task UpdateImage(string id, string type, string filename);
    Task<bool> UserIsTeamPlayer(string uid, string gid, string tid);
}

public class GameService : _Service, IGameService
{
    IGameStore Store { get; }
    Defaults Defaults { get; }

    private readonly IGameHubBus _gameHub;
    private readonly ILockService _lockService;
    private readonly IPlayerStore _playerStore;

    public GameService(
        ILogger<GameService> logger,
        IMapper mapper,
        CoreOptions options,
        Defaults defaults,
        IGameHubBus gameHub,
        IGameStore store,
        ILockService lockService,
        IPlayerStore playerStore
    ) : base(logger, mapper, options)
    {
        Store = store;
        Defaults = defaults;
        _gameHub = gameHub;
        _lockService = lockService;
        _playerStore = playerStore;
    }

    public async Task<Game> Create(NewGame model)
    {
        // for "New Game" only, set global defaults, if defined
        if (!model.IsClone)
        {
            if (Defaults.FeedbackTemplate.NotEmpty())
                model.FeedbackConfig = Defaults.FeedbackTemplate;
            if (Defaults.CertificateTemplate.NotEmpty())
                model.CertificateTemplate = Defaults.CertificateTemplate;
        }

        var entity = Mapper.Map<Data.Game>(model);

        await Store.Create(entity);

        return Mapper.Map<Game>(entity);
    }

    public async Task<Game> Retrieve(string id, bool accessHidden = true)
    {
        var game = await Store.Retrieve(id);
        if (!accessHidden && !game.IsPublished)
            throw new ActionForbidden();

        return Mapper.Map<Game>(game);
    }

    public async Task Update(ChangedGame account)
    {
        var entity = await Store.Retrieve(account.Id);

        Mapper.Map(account, entity);

        await Store.Update(entity);
    }

    public async Task Delete(string id)
    {
        await Store.Delete(id);
    }

    public IQueryable<Data.Game> BuildQuery(GameSearchFilter model = null, bool sudo = false)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var q = Store.List(model?.Term);

        if (!sudo)
            q = q.Where(g => g.IsPublished);

        if (model == null)
            return q;

        if (model.WantsPresent)
            q = q.Where(g => g.GameEnd > now && g.GameStart < now);

        if (model.WantsFuture)
            q = q.Where(g => g.GameStart > now);

        if (model.WantsPast)
            q = q.Where(g => g.GameEnd < now);

        if (model.WantsFuture)
            q = q.OrderBy(g => g.GameStart).ThenBy(g => g.Name);
        else
            q = q.OrderByDescending(g => g.GameStart).ThenBy(g => g.Name);

        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return q;
    }

    public async Task<IEnumerable<Game>> List(GameSearchFilter model = null, bool sudo = false)
    {
        var games = await BuildQuery(model, sudo)
            .ToArrayAsync();

        // Use Map instead of 'Mapper.ProjectTo<Game>' to support YAML parsing in automapper
        return Mapper.Map<IEnumerable<Game>>(games);
    }

    public async Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var q = Store.List(model.Term);

        if (!sudo)
            q = q.Where(g => g.IsPublished);

        if (model.WantsPresent)
            q = q.Where(g => g.GameEnd > now && g.GameStart < now);
        if (model.WantsFuture)
            q = q.Where(g => g.GameStart > now);
        if (model.WantsPast)
            q = q.Where(g => g.GameEnd < now);

        var games = await q.ToArrayAsync();

        var b = games
            .GroupBy(g => new
            {
                g.GameStart.Year,
                g.GameStart.Month,
            })
            .Select(g => new GameGroup
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Games = g
                    .OrderBy(c => c.GameStart)
                    .Select(c => Mapper.Map<Game>(c))
                    .ToArray()
            });

        if (model.WantsPast)
            b = b.OrderByDescending(g => g.Year).ThenByDescending(g => g.Month);
        else
            b = b.OrderBy(g => g.Year).ThenBy(g => g.Month);

        return b.ToArray();
    }

    public async Task<ChallengeSpec[]> RetrieveChallenges(string id)
    {
        var entity = await Store.Load(id);

        return Mapper.Map<ChallengeSpec[]>(
            entity.Specs
        );
    }

    public async Task<SessionForecast[]> SessionForecast(string id)
    {
        Data.Game entity = await Store.Retrieve(id);

        var ts = DateTimeOffset.UtcNow;
        var step = ts;

        var expirations = await Store.DbContext.Players
            .Where(p => p.GameId == id && p.Role == PlayerRole.Manager && p.SessionEnd.CompareTo(ts) > 0)
            .Select(p => p.SessionEnd)
            .ToArrayAsync();

        // foreach half hour, get count of available seats
        List<SessionForecast> result = new();

        for (int i = 0; i < 480; i += 30)
        {
            step = ts.AddMinutes(i);
            int reserved = expirations.Count(d => step.CompareTo(d) < 0);
            result.Add(new SessionForecast
            {
                Time = step,
                Reserved = reserved,
                Available = entity.SessionLimit - reserved
            });
        }

        return result.ToArray();
    }

    public async Task<string> Export(GameSpecExport model)
    {
        var yaml = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var entity = await Store.Retrieve(model.Id, q => q.Include(g => g.Specs));

        if (entity is Data.Game)
            return yaml.Serialize(entity);

        entity = new Data.Game
        {
            Id = Guid.NewGuid().ToString("n")
        };

        for (int i = 0; i < model.GenerateSpecCount; i++)
            entity.Specs.Add(new Data.ChallengeSpec
            {
                Id = Guid.NewGuid().ToString("n"),
                GameId = entity.Id
            });

        return model.Format == ExportFormat.Yaml
            ? yaml.Serialize(entity)
            : JsonSerializer.Serialize(entity, JsonOptions)
        ;

    }

    public async Task<Game> Import(GameSpecImport model)
    {
        var yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var entity = yaml.Deserialize<Data.Game>(model.Data);

        await Store.Create(entity);

        return Mapper.Map<Game>(entity);
    }

    public async Task UpdateImage(string id, string type, string filename)
    {
        var entity = await Store.Retrieve(id);

        switch (type)
        {
            case AppConstants.ImageMapType:
                entity.Background = filename;
                break;

            case AppConstants.ImageCardType:
                entity.Logo = filename;
                break;
        }

        await Store.Update(entity);
    }

    public async Task ReRank(string id)
    {
        var players = await Store.DbContext.Players
            .Where(p => p.GameId == id && p.Mode == PlayerMode.Competition)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Time)
            .ThenByDescending(p => p.CorrectCount)
            .ThenByDescending(p => p.PartialCount)
            .ToArrayAsync()
        ;

        int rank = 0;
        string last = "";
        foreach (var player in players)
        {
            if (player.TeamId != last)
            {
                rank += 1;
                last = player.TeamId;
            }

            player.Rank = rank;
        }

        await Store.DbContext.SaveChangesAsync();
    }

    public async Task<bool> UserIsTeamPlayer(string uid, string gid, string tid)
    {
        bool authd = await Store.DbContext.Users.AnyAsync(u =>
            u.Id == uid &&
            u.Enrollments.Any(e => e.TeamId == tid)
        );

        var players = Store.DbContext.Players.Where(p => p.UserId == uid);
        foreach (var e in players)
        {
            Console.WriteLine("game id: " + e.GameId + " | gid: " + gid);
        }

        return authd;
    }

    public async Task<SyncStartState> GetSyncStartState(string gameId)
    {
        var game = await Store.Retrieve(gameId);

        // a game and its challenges are "sync start ready" if either of the following are true:
        // - the game IS NOT a sync-start game
        // - the game IS sync-start game, and all registered players have set their IsReady flag to true.
        if (!game.RequireSynchronizedStart)
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = new SyncStartTeam[] { },
                IsReady = true
            };
        }

        // TODO: for some reason, clever uses of groupby and todictionaryasync aren't working like i expect them to.
        // they have stale properties. For example, compare the IsReady property of playersDebug here to the result of allTeamsReady
        // var playersDebug = await _playerStore
        //     .List()
        //     .Where(p => p.GameId == gameId)
        //     .Select(p => new
        //     {
        //         Id = p.Id,
        //         IsReady = p.IsReady
        //     })
        //     .ToListAsync();

        // var teams = new List<SyncStartTeam>();
        // var teamPlayers = await _playerStore
        //     .List()
        //     .Where(p => p.GameId == gameId)
        //     .GroupBy(p => p.TeamId)
        //     .ToDictionaryAsync(tp => tp.Key, tp => tp.ToList());
        // var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));

        // out of time, so for now, manually group on returned players
        var players = await _playerStore
            .List()
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .ToListAsync();

        var teams = new List<SyncStartTeam>();
        var teamPlayers = players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));

        return new SyncStartState
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Teams = teamPlayers.Keys.Select(teamId => new SyncStartTeam
            {
                Id = teamId,
                Name = teamPlayers[teamId].Single(p => p.Role == PlayerRole.Manager).ApprovedName,
                Players = teamPlayers[teamId].Select(p => new SyncStartPlayer
                {
                    Id = p.Id,
                    Name = p.ApprovedName,
                    IsReady = p.IsReady
                }),
                IsReady = teamPlayers[teamId].All(p => p.IsReady)
            }),
            IsReady = allTeamsReady
        };
    }

    public async Task HandleSyncStartStateChanged(string gameId, User actor)
    {
        var state = await GetSyncStartState(gameId);
        await _gameHub.SendSyncStartStateChanged(state, actor);

        // IFF everyone is ready, start all sessions and return info about them
        if (!state.IsReady)
            return;

        var session = await StartSynchronizedSession(gameId); ;
        await _gameHub.SendSyncStartGameStarting(session);
    }

    public async Task<SynchronizedGameStartedState> StartSynchronizedSession(string gameId)
    {
        using (await _lockService.GetSyncStartGameLock(gameId).LockAsync())
        {
            // make sure we have a legal sync start game
            var game = await Retrieve(gameId);

            if (!game.RequireSynchronizedStart)
                throw new CantSynchronizeNonSynchronizedGame(gameId);

            var state = await GetSyncStartState(gameId);
            if (!state.IsReady)
                throw new CantStartNonReadySynchronizedGame(gameId, state.Teams.SelectMany(t => t.Players).Where(p => !p.IsReady));

            // set the session times for all players
            var players = await _playerStore
                .List()
                .AsNoTracking()
                .Where(p => p.GameId == gameId)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = string.IsNullOrEmpty(p.ApprovedName) ? p.Name : p.ApprovedName,
                    SessionBegin = p.SessionBegin,
                    SessionEnd = p.SessionEnd,
                    TeamId = p.TeamId
                }).ToListAsync();

            // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
            // instead, we look at the minimum session start already set. this should be null for new games.if it's null, set everyone's
            // who doesn't have a session start to now plus something like 15 sec of lead time. 
            var playersWithSessions = players.Where(p => p.SessionBegin > DateTimeOffset.MinValue || p.SessionEnd > DateTimeOffset.MinValue);
            if (playersWithSessions.Count() > 0)
                throw new SynchronizedGameHasPlayersWithSessionsBeforeStart(game.Id, playersWithSessions.Select(p => p.Id));

            var sessionBegin = DateTimeOffset.UtcNow.AddSeconds(15);
            var sessionEnd = sessionBegin.AddMinutes(game.SessionMinutes);

            await _playerStore
                .List()
                .Where(p => p.GameId == gameId && p.SessionBegin == DateTimeOffset.MinValue)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.SessionBegin, sessionBegin)
                        .SetProperty(p => p.SessionEnd, sessionEnd)
                );


            var startState = new SynchronizedGameStartedState
            {
                Game = new SimpleEntity { Id = game.Id },
                SessionBegin = sessionBegin,
                SessionEnd = sessionEnd,
                Teams = players
                    .GroupBy(p => p.TeamId)
                    .ToDictionary(p => p.Key, p => p.Select(p => new SimpleEntity
                    {
                        Id = p.Id,
                        Name = p.Name
                    }))
            };

            await _gameHub.SendSyncStartGameStarting(startState);
            return startState;
        }
    }
}
