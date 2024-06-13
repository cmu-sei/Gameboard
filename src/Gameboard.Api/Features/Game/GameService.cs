// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Common.Services;
using Microsoft.AspNetCore.Http;
using Gameboard.Api.Data;
using System.IO;
using System.Threading;

namespace Gameboard.Api.Services;

public interface IGameService
{
    Task<Game> Create(NewGame model);
    Task Delete(string id);
    Task<string> Export(GameSpecExport model);
    Task<Game> Import(GameSpecImport model);
    Task<IEnumerable<string>> GetTeamsWithActiveSession(string GameId, CancellationToken cancellationToken);
    bool IsGameStartSuperUser(User user);
    IQueryable<Data.Game> BuildQuery(GameSearchFilter model = null, bool sudo = false);
    Task<bool> IsUserPlaying(string gameId, string userId);
    Task<IEnumerable<Game>> List(GameSearchFilter model = null, bool sudo = false);
    Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo);
    Task ReRank(string id);
    Task<Game> Retrieve(string id, bool accessHidden = true);
    Task<ChallengeSpec[]> RetrieveChallengeSpecs(string id);
    Task<SessionForecast[]> SessionForecast(string id);
    Task Update(ChangedGame account);
    Task UpdateImage(string id, string type, string filename);
    Task<bool> UserIsTeamPlayer(string uid, string gid, string tid);
}

public class GameService : _Service, IGameService
{
    private readonly Defaults _defaults;
    private readonly IGuidService _guids;
    private readonly IGameStore _gameStore;
    private readonly INowService _now;
    private readonly IStore _store;

    public GameService(
        IGuidService guids,
        ILogger<GameService> logger,
        IMapper mapper,
        CoreOptions options,
        Defaults defaults,
        IGameStore gameStore,
        INowService nowService,
        IStore store
    ) : base(logger, mapper, options)
    {
        _guids = guids;
        _gameStore = gameStore;
        _defaults = defaults;
        _now = nowService;
        _store = store;
    }

    public async Task<Game> Create(NewGame model)
    {
        // for "New Game" only, set global defaults, if defined
        if (!model.IsClone)
        {
            if (_defaults.FeedbackTemplate.NotEmpty())
                model.FeedbackConfig = _defaults.FeedbackTemplate;
            if (_defaults.CertificateTemplate.NotEmpty())
                model.CertificateTemplate = _defaults.CertificateTemplate;
        }

        // defaults: standard, 60 minutes, scoreboard access, etc.
        if (model.Mode.IsEmpty())
            model.Mode = GameEngineMode.Standard;

        // default to a session length of 60 minutes
        if (model.SessionMinutes == 0)
            model.SessionMinutes = 60;

        if (model.MinTeamSize == 0)
            model.MinTeamSize = 1;

        if (model.MaxTeamSize == 0)
            model.MaxTeamSize = 1;

        model.AllowPublicScoreboardAccess = true;

        var entity = Mapper.Map<Data.Game>(model);
        await _gameStore.Create(entity);
        return Mapper.Map<Game>(entity);
    }

    public async Task<Game> Retrieve(string id, bool accessHidden = true)
    {
        var game = await _gameStore.Retrieve(id);
        if (!accessHidden && !game.IsPublished)
            throw new ActionForbidden();

        return Mapper.Map<Game>(game);
    }

    public async Task Update(ChangedGame game)
    {
        if (game.Mode != GameEngineMode.External)
            game.ExternalHostId = null;

        var entity = await _gameStore.Retrieve(game.Id);
        Mapper.Map(game, entity);
        await _gameStore.Update(entity);
    }

    public Task Delete(string id)
        => _gameStore.Delete(id);


    public IQueryable<Data.Game> BuildQuery(GameSearchFilter model = null, bool sudo = false)
    {
        var q = _gameStore.List(model?.Term);
        var now = _now.Get();

        if (!sudo)
            q = q.Where(g => g.IsPublished);

        if (model == null)
            return q;

        if (model.IsFeatured.HasValue)
            q = q.Where(g => g.IsFeatured == model.IsFeatured);

        if (model.IsOngoing.HasValue)
            q = q.Where(g => (g.GameEnd == DateTimeOffset.MinValue) == model.IsOngoing);

        if (model.WantsAdvanceable)
            q = q.Where(g => g.GameEnd > now);

        if (model.WantsCompetitive)
            q = q.Where(g => g.PlayerMode == PlayerMode.Competition || g.ShowOnHomePageInPracticeMode);

        if (model.WantsPractice)
            q = q.Where(g => g.PlayerMode == PlayerMode.Practice);

        if (model.WantsPresent)
            q = q.Where(g => (g.GameEnd > now || g.GameEnd == AppConstants.NULL_DATE) && g.GameStart < now);

        if (model.WantsFuture)
            q = q.Where(g => g.GameStart > now);

        if (model.WantsPast)
            q = q.Where(g => g.GameEnd < now && g.GameEnd != AppConstants.NULL_DATE);

        if (model.OrderBy.IsNotEmpty() && model.OrderBy.ToLower() == "name")
            q = q.OrderBy(g => g.Name);
        else if (model.WantsFuture)
            q = q.OrderBy(g => g.GameStart).ThenBy(g => g.Name);
        else
            q = q.OrderByDescending(g => g.GameStart).ThenBy(g => g.Name);

        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return q;
    }

    public async Task<IEnumerable<string>> GetTeamsWithActiveSession(string gameId, CancellationToken cancellationToken)
    {
        var gameSessionData = await _store
            .WithNoTracking<Data.Game>()
                .Include(g => g.Players)
            .Where(g => g.Id == gameId)
            .Where(g => g.Players.Any(p => _now.Get() < p.SessionEnd))
            .Select(g => new
            {
                g.Id,
                g.SessionLimit,
                Teams = g
                    .Players
                    .Select(p => p.TeamId)
                    .Distinct()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (gameSessionData is not null)
            return gameSessionData.Teams;

        return Array.Empty<string>();
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

        var q = _gameStore.List(model.Term);

        if (!sudo)
            q = q.Where(g => g.IsPublished);

        if (model.WantsCompetitive)
            q = q.Where(g => g.PlayerMode == PlayerMode.Competition || g.ShowOnHomePageInPracticeMode);
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

    public async Task<ChallengeSpec[]> RetrieveChallengeSpecs(string id)
    {
        var entity = await _gameStore.Load(id);

        return Mapper.Map<ChallengeSpec[]>(entity.Specs)
            .OrderBy(s => s.Name)
            .ToArray();
    }

    public async Task<SessionForecast[]> SessionForecast(string id)
    {
        Data.Game entity = await _gameStore.Retrieve(id);

        var ts = DateTimeOffset.UtcNow;
        var step = ts;

        var expirations = await _gameStore.DbContext.Players
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

        var entity = await _gameStore.Retrieve(model.Id, q => q.Include(g => g.Specs));

        if (entity is not null)
            return yaml.Serialize(entity);

        entity = new Data.Game { Id = _guids.GetGuid() };

        for (int i = 0; i < model.GenerateSpecCount; i++)
            entity.Specs.Add(new Data.ChallengeSpec
            {
                Id = _guids.GetGuid(),
                GameId = entity.Id
            });

        return model.Format == ExportFormat.Yaml
            ? yaml.Serialize(entity)
            : JsonSerializer.Serialize(entity, JsonOptions);
    }

    public async Task<Game> Import(GameSpecImport model)
    {
        var yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var entity = yaml.Deserialize<Data.Game>(model.Data);

        await _gameStore.Create(entity);

        return Mapper.Map<Game>(entity);
    }

    public bool IsGameStartSuperUser(User user)
    {
        return user.IsAdmin || user.IsDesigner || user.IsRegistrar || user.IsSupport || user.IsTester;
    }

    public async Task UpdateImage(string id, string type, string filename)
    {
        var entity = await _gameStore.Retrieve(id);

        switch (type)
        {
            case AppConstants.ImageMapType:
                entity.Background = filename;
                break;

            case AppConstants.ImageCardType:
                entity.Logo = filename;
                break;
        }

        await _gameStore.Update(entity);
    }

    public async Task ReRank(string id)
    {
        var players = await _gameStore.DbContext.Players
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

        await _gameStore.DbContext.SaveChangesAsync();
    }

    public Task<bool> IsUserPlaying(string gameId, string userId)
        => _gameStore
            .DbContext
            .Players
            .AnyAsync(p => p.GameId == gameId && p.UserId == userId);

    public async Task<bool> UserIsTeamPlayer(string uid, string gid, string tid)
    {
        bool authd = await _gameStore.DbContext.Users.AnyAsync(u =>
            u.Id == uid &&
            u.Enrollments.Any(e => e.TeamId == tid)
        );

        return authd;
    }

    public async Task DeleteGameCardImage(string gameId)
    {
        if (!await _store.WithNoTracking<Data.Game>().AnyAsync(g => g.Id == gameId))
            throw new ResourceNotFound<Data.Game>(gameId);

        var fileSearchPattern = $"{GetGameCardFileNameBase(gameId)}.*";
        var files = Directory.GetFiles(Options.ImageFolder, fileSearchPattern);

        foreach (var cardImageFile in files)
            File.Delete(cardImageFile);

        await UpdateImage(gameId, "card", string.Empty);
    }

    public async Task<UploadedFile> SaveGameCardImage(string gameId, IFormFile file)
    {
        if (!await _store.WithNoTracking<Data.Game>().AnyAsync(g => g.Id == gameId))
            throw new ResourceNotFound<Data.Game>(gameId);

        var fileName = $"{GetGameCardFileNameBase(gameId)}{Path.GetExtension(file.FileName.ToLower())}";
        var path = Path.Combine(Options.ImageFolder, fileName);

        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        await UpdateImage(gameId, "card", fileName);

        return new UploadedFile { Filename = fileName };
    }

    private string GetGameCardFileNameBase(string gameId)
        => $"{gameId.ToLower()}_card";
}
