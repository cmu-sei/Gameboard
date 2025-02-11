// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;

namespace Gameboard.Api.Services;

public interface IGameService
{
    Task<Game> Create(NewGame model);
    Task DeleteGameCardImage(string gameId);
    IQueryable<GameActiveTeam> GetTeamsWithActiveSession(string GameId);
    Task<bool> IsUserPlaying(string gameId, string userId);
    Task<IEnumerable<Game>> List(GameSearchFilter model = null, bool sudo = false);
    Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo);
    Task<Game> Retrieve(string id, bool accessHidden = true);
    Task<ChallengeSpec[]> RetrieveChallengeSpecs(string id);
    Task<SessionForecast[]> SessionForecast(string id);
    Task<Data.Game> Update(ChangedGame account);
    Task UpdateImage(string id, string type, string filename);
    Task<bool> UserIsTeamPlayer(string uid, string tid);
}

public class GameService
(
    ILogger<GameService> logger,
    IMapper mapper,
    CoreOptions options,
    Defaults defaults,
    INowService nowService,
    IStore store
) : _Service(logger, mapper, options), IGameService
{
    private readonly Defaults _defaults = defaults;
    private readonly INowService _now = nowService;
    private readonly IStore _store = store;

    public async Task<Game> Create(NewGame model)
    {
        // for "New Game" only, set global defaults, if defined
        if (!model.IsClone)
        {
            if (_defaults.FeedbackTemplate.NotEmpty())
                model.FeedbackConfig = _defaults.FeedbackTemplate;
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
        var created = await _store.Create(entity);
        return Mapper.Map<Game>(created);
    }

    public async Task<Game> Retrieve(string id, bool accessHidden = true)
    {
        var game = await _store.SingleAsync<Data.Game>(id, default);
        if (!accessHidden && !game.IsPublished)
            throw new ActionForbidden();

        return Mapper.Map<Game>(game);
    }

    public async Task<Data.Game> Update(ChangedGame game)
    {
        if (game.Mode != GameEngineMode.External)
            game.ExternalHostId = null;

        var entity = await _store.WithTracking<Data.Game>().SingleAsync(g => g.Id == game.Id);
        Mapper.Map(game, entity);
        await _store.SaveUpdate(entity, default);

        return entity;
    }

    public IQueryable<GameActiveTeam> GetTeamsWithActiveSession(string gameId)
        => _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == gameId)
            .Where(p => p.SessionEnd > _now.Get())
            .GroupBy(p => p.TeamId)
            .Select(gr => new GameActiveTeam
            {
                TeamId = gr.Key,
                SessionEnd = gr.Min(p => p.SessionEnd)
            });

    public async Task<IEnumerable<Game>> List(GameSearchFilter model = null, bool sudo = false)
    {
        var games = await BuildSearchQuery(model, sudo).ToArrayAsync();

        return Mapper.Map<IEnumerable<Game>>(games);
    }

    public async Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo)
    {
        var query = BuildSearchQuery(model, sudo);
        var games = await query.ToArrayAsync();

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

        return [.. b];
    }

    public async Task<ChallengeSpec[]> RetrieveChallengeSpecs(string id)
    {
        var results = await Mapper.ProjectTo<ChallengeSpec>
        (
            _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Where(s => s.GameId == id)
        )
        .OrderBy(s => s.Name)
        .ToArrayAsync();

        return results;
    }

    public async Task<SessionForecast[]> SessionForecast(string id)
    {
        var gameInfo = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == id)
            .Select(g => new { g.Id, g.SessionLimit })
            .SingleAsync();

        var ts = DateTimeOffset.UtcNow;
        var step = ts;

        var expirations = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == id && p.Role == PlayerRole.Manager && p.SessionEnd.CompareTo(ts) > 0)
            .Select(p => p.SessionEnd)
            .ToArrayAsync();

        // foreach half hour, get count of available seats
        List<SessionForecast> result = [];

        for (int i = 0; i < 480; i += 30)
        {
            step = ts.AddMinutes(i);
            int reserved = expirations.Count(d => step.CompareTo(d) < 0);
            result.Add(new SessionForecast
            {
                Time = step,
                Reserved = reserved,
                Available = gameInfo.SessionLimit - reserved
            });
        }

        return [.. result];
    }

    public async Task UpdateImage(string id, string type, string filename)
    {
        var entity = await _store
            .WithTracking<Data.Game>()
            .SingleAsync(g => g.Id == id);

        switch (type)
        {
            case AppConstants.ImageMapType:
                entity.Background = filename;
                break;

            case AppConstants.ImageCardType:
                entity.Logo = filename;
                break;
        }

        await _store.SaveUpdate(entity, default);
    }

    public Task<bool> IsUserPlaying(string gameId, string userId)
        => _store.AnyAsync<Data.Player>(p => p.GameId == gameId && p.UserId == userId, CancellationToken.None);

    public async Task<bool> UserIsTeamPlayer(string uid, string tid)
    {
        bool authd = await _store.AnyAsync<Data.User>(u =>
            u.Id == uid &&
            u.Enrollments.Any(e => e.TeamId == tid)
        , CancellationToken.None);

        return authd;
    }

    public async Task DeleteGameCardImage(string gameId)
    {
        if (!await _store.WithNoTracking<Data.Game>().AnyAsync(g => g.Id == gameId))
            throw new ResourceNotFound<Data.Game>(gameId);

        var fileSearchPattern = $"{GetGameCardFileNameBase(gameId)}.*";
        var files = Directory.GetFiles(Options.ImageFolder, fileSearchPattern);

        foreach (var cardImageFile in files)
        {
            File.Delete(cardImageFile);
        }

        await UpdateImage(gameId, "card", string.Empty);
    }

    public async Task<UploadedFile> SaveGameCardImage(string gameId, IFormFile file)
    {
        if (!await _store.WithNoTracking<Data.Game>().AnyAsync(g => g.Id == gameId))
            throw new ResourceNotFound<Data.Game>(gameId);

        var fileName = $"{GetGameCardFileNameBase(gameId)}{Path.GetExtension(file.FileName.ToLower())}";
        var path = Path.Combine(Options.ImageFolder, fileName);

        using var stream = new FileStream(path, FileMode.OpenOrCreate);
        await file.CopyToAsync(stream);
        await UpdateImage(gameId, "card", fileName);

        return new UploadedFile { Filename = fileName };
    }

    private string GetGameCardFileNameBase(string gameId)
        => $"{gameId.ToLower()}_card";

    private IQueryable<Data.Game> BuildSearchQuery(GameSearchFilter model, bool canViewUnpublished = false)
    {
        var now = _now.Get();
        var q = _store
            .WithNoTracking<Data.Game>();

        if (!string.IsNullOrEmpty(model.Term))
        {
            var term = model.Term.ToLower();

            q = q.Where
            (
                t =>
                    t.Name.ToLower().Contains(term) ||
                    t.Season.ToLower().Contains(term) ||
                    t.Track.ToLower().Contains(term) ||
                    t.Division.ToLower().Contains(term) ||
                    t.Competition.ToLower().Contains(term) ||
                    t.Sponsor.ToLower().Contains(term) ||
                    t.Mode.ToLower().Contains(term) ||
                    t.Id.ToLower().StartsWith(term) ||
                    t.CardText1.ToLower().Contains(term) ||
                    t.CardText2.ToLower().Contains(term) ||
                    t.CardText3.ToLower().Contains(term)
            );
        }

        if (!canViewUnpublished)
            q = q.Where(g => g.IsPublished);

        if (model == null)
            return q.OrderBy(g => g.Name);

        if (model.IsFeatured.HasValue)
            q = q.Where(g => g.IsFeatured == model.IsFeatured);

        if (model.IsOngoing.HasValue)
            q = q
                .Where(g => g.GameEnd == DateTimeOffset.MinValue == model.IsOngoing)
                .Where(g => g.PlayerMode == PlayerMode.Competition);

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
}
