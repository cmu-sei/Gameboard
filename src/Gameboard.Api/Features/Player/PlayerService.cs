// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Services;

public class PlayerService
{
    private readonly IPracticeChallengeScoringListener _practiceChallengeScoringListener;
    private readonly TimeSpan _idmapExpiration = new(0, 30, 0);
    private readonly INowService _now;
    private readonly IPracticeService _practiceService;

    CoreOptions CoreOptions { get; }
    ChallengeService ChallengeService { get; set; }
    IPlayerStore PlayerStore { get; }
    IGameService GameService { get; }
    IGameStore GameStore { get; }
    IGuidService GuidService { get; }
    IInternalHubBus HubBus { get; }
    ITeamService TeamService { get; }
    IMapper Mapper { get; }
    IMemoryCache LocalCache { get; }

    public PlayerService(
        ChallengeService challengeService,
        CoreOptions coreOptions,
        IGuidService guidService,
        INowService now,
        IPlayerStore playerStore,
        IGameService gameService,
        IGameStore gameStore,
        IInternalHubBus hubBus,
        IPracticeChallengeScoringListener practiceChallengeScoringListener,
        IPracticeService practiceService,
        ITeamService teamService,
        IMapper mapper,
        IMemoryCache localCache
    )
    {
        ChallengeService = challengeService;
        CoreOptions = coreOptions;
        GameService = gameService;
        GuidService = guidService;
        _practiceChallengeScoringListener = practiceChallengeScoringListener;
        _practiceService = practiceService;
        _now = now;
        HubBus = hubBus;
        PlayerStore = playerStore;
        GameStore = gameStore;
        TeamService = teamService;
        Mapper = mapper;
        LocalCache = localCache;
    }

    public async Task<Player> Enroll(NewPlayer model, User actor, CancellationToken cancellationToken)
    {
        var game = await GameStore.Retrieve(model.GameId);

        if (actor.Sponsor.IsEmpty())
            throw new NoPlayerSponsorForGame(model.UserId, model.GameId);

        if (game.IsPracticeMode)
            return await RegisterPracticeSession(model, cancellationToken);

        if (!actor.IsRegistrar && !game.RegistrationActive)
            throw new RegistrationIsClosed(model.GameId);

        var user = await PlayerStore.GetUserEnrollments(model.UserId);

        if (user.Enrollments.Any(p => p.GameId == model.GameId))
            throw new AlreadyRegistered(model.UserId, model.GameId);

        var entity = await InitializePlayer(model, game.SessionMinutes);

        await PlayerStore.Create(entity);
        await HubBus.SendPlayerEnrolled(Mapper.Map<Api.Player>(entity), actor);

        if (game.RequireSynchronizedStart)
            await GameService.HandleSyncStartStateChanged(entity.GameId, actor);

        return Mapper.Map<Player>(entity);
    }

    /// <summary>
    /// Maps a PlayerId to its UserId
    /// </summary>
    /// <remarks>This happens frequently for authorization, so cache the mapping.</remarks>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public async Task<string> MapId(string playerId)
    {
        if (LocalCache.TryGetValue(playerId, out string userId))
            return userId;

        userId = (await PlayerStore.Retrieve(playerId))?.UserId;
        LocalCache.Set(playerId, userId, _idmapExpiration);

        return userId;
    }

    public async Task<Player> Retrieve(string id)
    {
        return Mapper.Map<Player>(await PlayerStore.Retrieve(id));
    }

    public async Task<Player> Update(ChangedPlayer model, User actor, bool sudo = false)
    {
        var entity = await PlayerStore.Retrieve(model.Id);
        var prev = Mapper.Map<Player>(entity);

        if (!sudo)
        {
            Mapper.Map(
                Mapper.Map<SelfChangedPlayer>(model),
                entity
            );

            entity.NameStatus = entity.Name != entity.ApprovedName ? "pending" : string.Empty;
        }
        else
        {
            Mapper.Map(model, entity);
        }

        if (prev.Name != entity.Name)
        {
            // check uniqueness
            bool found = await PlayerStore.DbSet.AnyAsync(p =>
                p.GameId == entity.GameId &&
                p.TeamId != entity.TeamId &&
                p.Name == entity.Name
            );

            if (found)
                entity.NameStatus = AppConstants.NameStatusNotUnique;
        }

        await PlayerStore.Update(entity);
        await HubBus.SendTeamUpdated(Mapper.Map<Api.Player>(entity), actor);
        return Mapper.Map<Player>(entity);
    }

    public async Task UpdatePlayerReadyState(string playerId, bool isReady)
    {
        var player = await PlayerStore.Retrieve(playerId);
        await PlayerStore
            .List()
            .Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.IsReady, isReady));
    }

    public async Task<Player> ResetSession(SessionResetCommandArgs args)
    {
        var player = await PlayerStore
            .DbSet
            .AsNoTracking()
            .Include(p => p.Game)
            .SingleAsync(p => p.Id == args.PlayerId);

        // always archive challenges
        await ChallengeService.ArchiveTeamChallenges(player.TeamId);

        // delete the entire team if requested
        if (args.UnenrollTeam)
        {
            await PlayerStore.DeleteTeam(player.TeamId);

            // notify hub that the team is deleted /players left so the client can respond
            var playerModel = Mapper.Map<Player>(player);
            await HubBus.SendTeamDeleted(playerModel, args.ActingUser);

            if (!player.IsManager && !player.Game.RequireSponsoredTeam)
                await TeamService.UpdateTeamSponsors(player.TeamId);
        }

        // update player ready state if game needs it
        if (player.Game.RequireSynchronizedStart && player.SessionBegin == DateTimeOffset.MinValue)
            await GameService.HandleSyncStartStateChanged(player.GameId, args.ActingUser);

        return Mapper.Map<Player>(player);
    }

    public async Task<Player> StartSession(SessionStartRequest model, User actor, bool sudo)
    {
        var team = await PlayerStore.ListTeamByPlayer(model.PlayerId);

        var player = team.First();
        var game = await PlayerStore.DbContext.Games.SingleOrDefaultAsync(g => g.Id == player.GameId);

        // rule: game's execution period has to be open
        if (!sudo && game.IsLive.Equals(false))
            throw new GameNotActive();

        // rule: players per team has to be within the game's constraint
        if (
            !sudo &&
            game.RequireTeam &&
            team.Length < game.MinTeamSize
        )
            throw new InvalidTeamSize();

        // rule: for now, can't start a player's session in this code path.
        // TODO: refactor for SOLIDness
        if (!sudo && game.RequireSynchronizedStart)
        {
            throw new InvalidOperationException("Can't start a player's session for a sync start game with PlayerService.StartSession (use GameService.StartSynchronizedSession).");
        }

        // rule: teams can't have a session limit exceeding the game's settings
        if (!sudo && game.SessionLimit > 0)
        {
            var ts = DateTimeOffset.UtcNow;

            int sessionCount = await PlayerStore.DbSet
                .CountAsync(p =>
                    p.GameId == game.Id &&
                    p.Role == PlayerRole.Manager &&
                    ts < p.SessionEnd
                );

            if (sessionCount >= game.SessionLimit)
                throw new SessionLimitReached(player.TeamId, game.Id, sessionCount, game.SessionLimit);
        }

        var st = DateTimeOffset.UtcNow;
        var et = st.AddMinutes(game.SessionMinutes);

        foreach (var p in team)
        {
            p.SessionMinutes = game.SessionMinutes;
            p.SessionBegin = st;
            p.SessionEnd = et;
        }

        await PlayerStore.Update(team);

        if (player.Score > 0)
        {
            var challenge = new Data.Challenge
            {
                Id = Guid.NewGuid().ToString("n"),
                PlayerId = player.Id,
                TeamId = player.TeamId,
                GameId = player.GameId,
                SpecId = "_initialscore_",
                Name = "_initialscore_",
                Points = player.Score,
                Score = player.Score,
            };

            PlayerStore.DbContext.Add(challenge);
            await PlayerStore.DbContext.SaveChangesAsync();
        }

        var asViewModel = Mapper.Map<Player>(player);
        await HubBus.SendTeamSessionStarted(asViewModel, actor);

        return asViewModel;
    }

    public async Task<Player[]> List(PlayerDataFilter model, bool sudo = false)
    {
        if (!sudo && !model.WantsGame && !model.WantsTeam)
            return Array.Empty<Player>();

        var q = BuildListQuery(model);

        return await Mapper.ProjectTo<Player>(q).ToArrayAsync();
    }

    public async Task<Standing[]> Standings(PlayerDataFilter model)
    {
        if (model.gid.IsEmpty())
            return Array.Empty<Standing>();

        model.Filter = model.Filter
            .Append(PlayerDataFilter.FilterScoredOnly)
            .ToArray()
        ;

        model.mode = PlayerMode.Competition.ToString();

        var q = BuildListQuery(model);

        return await Mapper.ProjectTo<Standing>(q).ToArrayAsync();
    }

    private IQueryable<Data.Player> BuildListQuery(PlayerDataFilter model)
    {
        var ts = DateTimeOffset.UtcNow;

        var q = PlayerStore.List()
            .Include(p => p.User)
            .AsNoTracking();

        if (model.WantsMode)
            q = q.Where(p => p.Mode == Enum.Parse<PlayerMode>(model.mode, true));

        if (model.WantsGame)
        {
            q = q.Where(p => p.GameId == model.gid);

            if (model.WantsOrg)
                q = q.Where(p => p.Sponsor == model.org);
        }

        if (model.WantsUser)
            q = q.Where(p => p.UserId == model.uid);

        if (model.WantsTeam)
            q = q.Where(p => p.TeamId == model.tid);

        if (model.WantsCollapsed || model.WantsActive || model.WantsScored)
            q = q.Where(p => p.Role == PlayerRole.Manager);

        if (model.WantsActive)
            q = q.Where(p => p.SessionBegin < ts && p.SessionEnd > ts);

        if (model.WantsComplete)
            q = q.Where(p => p.SessionEnd > DateTimeOffset.MinValue);

        if (model.WantsAdvanced)
            q = q.Where(p => p.Advanced);

        if (model.WantsDismissed)
            q = q.Where(p => !p.Advanced);

        if (model.WantsPending)
            q = q.Where(u => u.NameStatus.Equals(AppConstants.NameStatusPending));

        if (model.WantsDisallowed)
            q = q.Where(u => !string.IsNullOrEmpty(u.NameStatus) && !u.NameStatus.Equals(AppConstants.NameStatusPending));

        if (model.WantsScored)
            q = q.WhereIsScoringPlayer();

        if (model.Term.NotEmpty())
        {
            string term = model.Term.ToLower();

            q = q.Where(p =>
                p.ApprovedName.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term) ||
                p.Id.StartsWith(term) ||
                p.TeamId.StartsWith(term) ||
                p.UserId.StartsWith(term) ||
                p.Sponsor.StartsWith(term) ||
                p.User.Name.ToLower().Contains(term) ||
                p.User.ApprovedName.ToLower().Contains(term) ||
                PlayerStore.DbSet.Where(p2 => p2.TeamId == p.TeamId && (p2.UserId.StartsWith(term) || p2.User.ApprovedName.ToLower().Contains(term))).Any()
            );
        }

        // TODO: maybe just sort on rank here
        if (model.WantsSortByRank)
            q = q.OrderByDescending(p => p.Score)
                .ThenBy(p => p.Time)
                .ThenByDescending(p => p.CorrectCount)
                .ThenByDescending(p => p.PartialCount)
                .ThenBy(p => p.Rank)
                .ThenBy(p => p.ApprovedName);

        if (model.WantsSortByTime)
            q = q.OrderByDescending(p => p.SessionBegin);

        if (model.WantsSortByName)
            q = q.OrderBy(p => p.ApprovedName)
                .ThenBy(p => p.User.ApprovedName);

        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return q;
    }

    public async Task<BoardPlayer> LoadBoard(string id)
    {
        var mapped = Mapper.Map<BoardPlayer>(
            await PlayerStore.LoadBoard(id)
        );

        mapped.ChallengeDocUrl = CoreOptions.ChallengeDocUrl;
        return mapped;
    }

    public async Task<TeamInvitation> GenerateInvitation(string id)
    {
        var player = await PlayerStore.Retrieve(id);

        if (player.Role != PlayerRole.Manager)
            throw new ActionForbidden();

        byte[] buffer = new byte[16];

        new Random().NextBytes(buffer);

        player.InviteCode = Convert.ToBase64String(buffer)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
        ;

        await PlayerStore.Update(player);

        return new TeamInvitation
        {
            Code = player.InviteCode
        };
    }

    public async Task<Player> Enlist(PlayerEnlistment model, User actor)
    {
        var sudo = actor.IsRegistrar;
        var manager = await PlayerStore.List()
            .Include(p => p.Game)
            .FirstOrDefaultAsync(
                p => p.InviteCode == model.Code
            );

        var player = await PlayerStore.DbSet.FirstOrDefaultAsync(p => p.Id == model.PlayerId);

        if (player is null)
        {
            throw new ResourceNotFound<Player>(model.PlayerId);
        }

        if (player.GameId != manager.GameId)
            throw new NotYetRegistered(player.Id, manager.GameId);

        if (manager is not Data.Player)
            throw new InvalidInvitationCode(model.Code, "Couldn't find the manager record.");

        if (player.Id == manager.Id)
            return Mapper.Map<Player>(player);

        if (!sudo && !manager.Game.RegistrationActive)
            throw new RegistrationIsClosed(manager.GameId);

        if (!sudo && manager.SessionBegin.Year > 1)
            throw new RegistrationIsClosed(manager.GameId, "Registration begins in more than a year.");

        if (!sudo && manager.Game.RequireSponsoredTeam && !manager.Sponsor.Equals(player.Sponsor))
            throw new RequiresSameSponsor(manager.GameId, manager.Id, manager.Sponsor.Name, player.Id, player.Sponsor);

        int count = await PlayerStore.List().CountAsync(p => p.TeamId == manager.TeamId);

        if (!sudo && manager.Game.AllowTeam && count >= manager.Game.MaxTeamSize)
            throw new TeamIsFull(manager.Id, count, manager.Game.MaxTeamSize);

        player.TeamId = manager.TeamId;
        player.Role = PlayerRole.Member;
        player.InviteCode = model.Code;

        await PlayerStore.Update(player);

        if (manager.Game.AllowTeam && !manager.Game.RequireSponsoredTeam)
            await TeamService.UpdateTeamSponsors(manager.TeamId);

        var mappedPlayer = Mapper.Map<Player>(player);
        await HubBus.SendPlayerEnrolled(mappedPlayer, actor);
        return mappedPlayer;
    }

    public async Task Unenroll(PlayerUnenrollRequest request)
    {
        // they probably don't have challenge data on an unenroll, but in case an admin does this
        // or something, we'll clean up their challenges
        var player = await PlayerStore.Retrieve(request.PlayerId, players => players.Include(p => p.Game));
        await ChallengeService.ArchivePlayerChallenges(player);

        // delete the player record
        await PlayerStore.Delete(request.PlayerId);

        // manage sponsor info about the team
        await TeamService.UpdateTeamSponsors(player.TeamId);

        // notify listeners on SignalR (like the team)
        var playerModel = Mapper.Map<Player>(player);
        await HubBus.SendPlayerLeft(playerModel, request.Actor);

        // update sync start if needed
        if (player.Game.RequireSynchronizedStart)
            await GameService.HandleSyncStartStateChanged(player.GameId, request.Actor);
    }

    public async Task<TeamChallenge[]> LoadChallengesForTeam(string teamId)
    {
        return Mapper.Map<TeamChallenge[]>(await PlayerStore.ListTeamChallenges(teamId));
    }

    public async Task<TeamSummary[]> LoadTeams(string id, bool sudo)
    {
        var players = await PlayerStore.List()
            .Where(p => p.GameId == id)
            .ToArrayAsync();

        var teams = players
            .GroupBy(p => p.TeamId)
            .Select(g => new TeamSummary
            {
                Id = g.Key,
                Name = g.First().ApprovedName,
                Sponsor = g.First().Sponsor,
                Members = g.Select(i => i.UserId).ToArray()
            })
            .ToArray()
        ;

        return teams;
    }

    public async Task<IEnumerable<Team>> ObserveTeams(string id)
    {
        var players = await PlayerStore.List()
            .Where(p => p.GameId == id)
            .Include(p => p.User)
            .ToArrayAsync();

        var captains = players
            .Where(p => p.IsManager)
            .Where(p => p.IsLive)
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.First());

        var teams = captains
            .Values
            .Select(c => new Team
            {
                TeamId = c.TeamId,
                ApprovedName = c.ApprovedName,
                Sponsor = c.Sponsor.Logo,
                GameId = c.GameId,
                SessionBegin = c.SessionBegin,
                SessionEnd = c.SessionEnd,
                Rank = c.Rank,
                Score = c.Score,
                Time = c.Time,
                CorrectCount = c.CorrectCount,
                PartialCount = c.PartialCount,
                Advanced = c.Advanced,
                Members = players.Where(p => p.TeamId == c.TeamId).Select(i => new TeamMember
                {
                    Id = i.UserId,
                    ApprovedName = i.User.ApprovedName,
                    Role = i.Role
                }).OrderBy(p => p.ApprovedName).ToArray()
            })
            .OrderBy(c => c.ApprovedName)
            .ToArray();

        return teams;
    }

    public async Task AdvanceTeams(TeamAdvancement model)
    {
        var game = await GameStore.Retrieve(model.NextGameId);

        var allteams = await PlayerStore.List()
            .Where(p => p.GameId == model.GameId)
            .ToArrayAsync()
        ;

        var teams = allteams.GroupBy(p => p.TeamId)
            .Where(g => model.TeamIds.Contains(g.Key))
            .ToArray()
        ;

        var enrollments = new List<Data.Player>();

        foreach (var team in teams)
        {
            string newId = Guid.NewGuid().ToString("n");

            foreach (var player in team)
            {
                player.Advanced = true;

                enrollments.Add(new Data.Player
                {
                    TeamId = newId,
                    UserId = player.UserId,
                    GameId = model.NextGameId,
                    ApprovedName = player.ApprovedName,
                    Name = player.Name,
                    Sponsor = player.Sponsor,
                    Role = player.Role,
                    Score = model.WithScores ? player.Score : 0
                });

                if (player.IsManager)
                {
                    player.TeamSponsors = string.Join('|', team
                        .Select(p => p.Sponsor.Id)
                        .Distinct()
                        .ToArray()
                    );
                }
            }
        }

        await PlayerStore.Create(enrollments);
        await PlayerStore.Update(allteams);
    }

    public Task<Player> AdjustSessionEnd(SessionChangeRequest model, User actor, CancellationToken cancellationToken)
        => _practiceChallengeScoringListener.AdjustSessionEnd(model, actor, cancellationToken);

    public async Task<PlayerCertificate> MakeCertificate(string id)
    {
        var player = await PlayerStore.List()
            .Include(p => p.Game)
            .Include(p => p.User)
                .ThenInclude(u => u.PublishedCompetitiveCertificates)
            .FirstOrDefaultAsync(p => p.Id == id);

        var playerCount = await PlayerStore.DbSet
            .Where(p => p.GameId == player.GameId &&
                p.SessionEnd > DateTimeOffset.MinValue)
            .CountAsync();

        var teamCount = await PlayerStore.DbSet
            .Where(p => p.GameId == player.GameId &&
                p.SessionEnd > DateTimeOffset.MinValue)
            .GroupBy(p => p.TeamId)
            .CountAsync();

        return CertificateFromTemplate(player, playerCount, teamCount);
    }

    public async Task<IEnumerable<PlayerCertificate>> MakeCertificates(string uid)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var completedSessions = await PlayerStore.List()
            .Include(p => p.Game)
            .Include(p => p.User)
                .ThenInclude(u => u.PublishedCompetitiveCertificates)
            .Where
            (
                p => p.UserId == uid &&
                p.SessionEnd > DateTimeOffset.MinValue &&
                p.Game.GameEnd < now &&
                p.Game.CertificateTemplate != null &&
                p.Game.CertificateTemplate.Length > 0
            )
            .WhereIsScoringPlayer()
            .OrderByDescending(p => p.Game.GameEnd)
            .ToArrayAsync();

        return completedSessions.Select(c => CertificateFromTemplate(c,
            PlayerStore.DbSet
                .Where(p => p.Game == c.Game &&
                    p.SessionEnd > DateTimeOffset.MinValue)
                .WhereIsScoringPlayer()
                .Count(),
            PlayerStore.DbSet
                .Where(p => p.Game == c.Game &&
                    p.SessionEnd > DateTimeOffset.MinValue)
                .WhereIsScoringPlayer()
                .GroupBy(p => p.TeamId).Count()
        )).ToArray();
    }

    private PlayerCertificate CertificateFromTemplate(Data.Player player, int playerCount, int teamCount)
    {
        string certificateHTML = player.Game.CertificateTemplate;
        if (certificateHTML.IsEmpty())
            return null;

        certificateHTML = certificateHTML.Replace("{{leaderboard_name}}", player.ApprovedName);
        certificateHTML = certificateHTML.Replace("{{user_name}}", player.User.ApprovedName);
        certificateHTML = certificateHTML.Replace("{{score}}", player.Score.ToString());
        certificateHTML = certificateHTML.Replace("{{rank}}", player.Rank.ToString());
        certificateHTML = certificateHTML.Replace("{{game_name}}", player.Game.Name);
        certificateHTML = certificateHTML.Replace("{{competition}}", player.Game.Competition);
        certificateHTML = certificateHTML.Replace("{{season}}", player.Game.Season);
        certificateHTML = certificateHTML.Replace("{{track}}", player.Game.Track);
        certificateHTML = certificateHTML.Replace("{{date}}", player.SessionEnd.ToString("MMMM dd, yyyy"));
        certificateHTML = certificateHTML.Replace("{{player_count}}", playerCount.ToString());
        certificateHTML = certificateHTML.Replace("{{team_count}}", teamCount.ToString());

        return new Api.PlayerCertificate
        {
            Game = Mapper.Map<Game>(player.Game),
            PublishedOn = player.User.PublishedCompetitiveCertificates.FirstOrDefault(c => c.GameId == player.Game.Id)?.PublishedOn,
            Player = Mapper.Map<Player>(player),
            Html = certificateHTML
        };
    }

    private async Task<Player> RegisterPracticeSession(NewPlayer model, CancellationToken cancellationToken)
    {
        // load practice settings
        var settings = await _practiceService.GetSettings(cancellationToken);

        // check for existing sessions
        var nowStamp = _now.Get();

        var players = await PlayerStore.DbContext.Players.Where(p =>
            p.UserId == model.UserId &&
            p.Mode == PlayerMode.Practice &&
            p.SessionEnd > nowStamp
        ).ToArrayAsync();

        if (players.Any(p => p.GameId == model.GameId))
            return Mapper.Map<Player>(players.First(p => p.GameId == model.GameId));

        // find gamespaces across all practice sessions
        var teamIds = players.Select(p => p.TeamId).ToArray();

        bool hasGamespace = await PlayerStore.DbContext.Challenges.AnyAsync(c =>
            teamIds.Contains(c.TeamId) &&
            c.HasDeployedGamespace == true
        );

        // only 1 practice gamespace at a time
        if (hasGamespace)
            throw new GamespaceLimitReached();

        // don't exceed global configured limit
        if (settings.MaxConcurrentPracticeSessions.HasValue)
        {
            int count = await PlayerStore.DbSet.CountAsync(p =>
                p.Mode == PlayerMode.Practice &&
                p.SessionEnd > nowStamp, cancellationToken);

            if (count >= settings.MaxConcurrentPracticeSessions.Value)
                throw new PracticeSessionLimitReached(model.UserId, count, settings.MaxConcurrentPracticeSessions.Value);
        }

        var entity = await InitializePlayer(model, settings.DefaultPracticeSessionLengthMinutes);

        // start session
        entity.SessionBegin = nowStamp;
        entity.SessionEnd = entity.SessionBegin.AddMinutes(entity.SessionMinutes);
        entity.Mode = PlayerMode.Practice;

        await PlayerStore.Create(entity);

        return Mapper.Map<Player>(entity);
    }

    private async Task<Data.Player> InitializePlayer(NewPlayer model, int duration)
    {
        var user = await PlayerStore.DbContext.Users.FindAsync(model.UserId);

        var entity = Mapper.Map<Data.Player>(model);
        entity.TeamId = GuidService.GetGuid();
        entity.Role = PlayerRole.Manager;
        entity.ApprovedName = user.ApprovedName;
        entity.Name = user.ApprovedName;
        entity.Sponsor = user.Sponsor;
        entity.SessionMinutes = duration;
        entity.WhenCreated = _now.Get();

        return entity;
    }
}
