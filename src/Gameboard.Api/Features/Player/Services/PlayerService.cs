// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Sponsors;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public class PlayerService
{
    private readonly IGuidService _guids;
    private readonly IInternalHubBus _hubBus;
    private readonly TimeSpan _idmapExpiration = new(0, 30, 0);
    private readonly ILogger<PlayerService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IUserRolePermissionsService _permissionsService;
    private readonly IPracticeService _practiceService;
    private readonly IScoringService _scores;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;

    CoreOptions CoreOptions { get; }
    ChallengeService ChallengeService { get; set; }
    IGuidService GuidService { get; }
    IMemoryCache LocalCache { get; }

    public PlayerService
    (
        ChallengeService challengeService,
        CoreOptions coreOptions,
        IGuidService guidService,
        IInternalHubBus hubBus,
        ILogger<PlayerService> logger,
        IMapper mapper,
        IMediator mediator,
        IMemoryCache memCache,
        INowService now,
        IUserRolePermissionsService permissionsService,
        IPracticeService practiceService,
        IScoringService scores,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService
    )
    {
        ChallengeService = challengeService;
        CoreOptions = coreOptions;
        CoreOptions = coreOptions;
        GuidService = guidService;

        _mediator = mediator;
        _practiceService = practiceService;
        _now = now;
        _guids = guidService;
        _hubBus = hubBus;
        _logger = logger;
        LocalCache = memCache;
        _mapper = mapper;
        _permissionsService = permissionsService;
        _scores = scores;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
    }

    public async Task<Player> Enroll(NewPlayer model, User actor, CancellationToken cancellationToken)
    {
        var canIgnoreRegistrationRequirements = await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow);
        var game = await _store
            .WithNoTracking<Data.Game>()
            .SingleAsync(g => g.Id == model.GameId, default);
        var user = await _store
            .WithNoTracking<Data.User>()
            .Include(u => u.Sponsor)
            // include registrations for this game and type (because we validate whether they have active registrations later)
            .Include(u => u.Enrollments.Where(p => p.GameId == model.GameId && p.Mode == game.PlayerMode))
            .SingleAsync(u => u.Id == model.UserId, cancellationToken);

        if (user.HasDefaultSponsor)
            throw new CantEnrollWithDefaultSponsor(model.UserId, model.GameId);

        if (user.SponsorId.IsEmpty())
            throw new NoPlayerSponsorForGame(model.UserId, model.GameId);

        if (game.IsPracticeMode)
            return await RegisterPracticeSession(model, user, cancellationToken);

        if (!game.RegistrationActive && !canIgnoreRegistrationRequirements)
            throw new RegistrationIsClosed(model.GameId);

        // while this collection will always only contain the correct player records (because of the filtered include above),
        // we have to specify our criteria again here because mock providers for unit tests seem to ignore filtered includes
        if (user.Enrollments.Any(p => p.GameId == game.Id && p.Mode == game.PlayerMode))
            throw new AlreadyRegistered(model.UserId, model.GameId);

        var entity = InitializePlayer(model, user, game.SessionMinutes);

        await _store.Create(entity, cancellationToken);
        await _hubBus.SendPlayerEnrolled(_mapper.Map<Player>(entity), actor);
        await _mediator.Publish(new GameEnrolledPlayersChangeNotification(new GameEnrolledPlayersChangeContext(entity.GameId, game.RequireSynchronizedStart)), cancellationToken);

        if (game.RequireSynchronizedStart)
            await _syncStartGameService.HandleSyncStartStateChanged(entity.GameId, cancellationToken);

        // the initialized Data.Player only has the SponsorId, and we want to send down the complete
        // sponsor object. We could just manually attach it, but for now we're just going to reload
        // the entity from the DB to handle future property wireups
        return _mapper.Map<Player>
        (
            await _store
                .WithNoTracking<Data.Player>()
                .Include(p => p.Sponsor)
                .SingleAsync(p => p.Id == entity.Id, cancellationToken)
        );
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

        var playerIdWithUserId = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == playerId)
            .Select(p => new { PlayerId = p.Id, p.UserId })
            .SingleOrDefaultAsync();

        if (playerIdWithUserId is not null)
        {
            LocalCache.Set(playerIdWithUserId.PlayerId, playerIdWithUserId.UserId, _idmapExpiration);
        }

        return playerIdWithUserId?.UserId;
    }

    public async Task<Player> Retrieve(string id)
    {
        return _mapper.Map<Player>(await _store.WithNoTracking<Data.Player>().SingleAsync(p => p.Id == id));
    }

    public async Task<Player> Update(ChangedPlayer model, User actor, bool sudo = false)
    {
        var player = await _store
            .WithNoTracking<Data.Player>()
            .SingleAsync(p => p.Id == model.Id);
        var prev = _mapper.Map<Player>(player);

        if (!sudo)
        {
            _mapper.Map(
                _mapper.Map<SelfChangedPlayer>(model),
                player
            );
        }
        else
        {
            _mapper.Map(model, player);
        }

        // if manipulation of the names has caused Name to equal ApprovedName, clear any pending status
        if (player.Name == player.ApprovedName && player.NameStatus == AppConstants.NameStatusPending)
            player.NameStatus = string.Empty;

        if (prev.Name != player.Name)
        {
            // check uniqueness
            bool found = await _store
                .WithNoTracking<Data.Player>()
                .AnyAsync(p =>
                p.GameId == player.GameId &&
                p.TeamId != player.TeamId &&
                p.Name == player.Name
            );

            if (found)
                player.NameStatus = AppConstants.NameStatusNotUnique;
        }

        // save
        await _store.SaveUpdate(player, CancellationToken.None);

        // notify and return
        var mappedDto = _mapper.Map<Player>(player);
        await _hubBus.SendTeamUpdated(mappedDto, actor);
        return mappedDto;
    }

    public async Task<Player> StartSession(SessionStartRequest model, User actor, bool sudo)
    {
        if (model.PlayerId.IsEmpty())
            throw new MissingRequiredInput<string>(nameof(model.PlayerId), model.PlayerId);

        var startingPlayer = await _store
            .WithNoTracking<Data.Player>()
            .SingleOrDefaultAsync(p => p.Id == model.PlayerId);

        var result = await _mediator.Send(new StartTeamSessionsCommand(new string[] { startingPlayer.TeamId }));

        // also set the starting player's properties because we'll use them as a return
        var teamStartResult = result.Teams[startingPlayer.TeamId];
        startingPlayer.IsLateStart = teamStartResult.SessionWindow.IsLateStart;
        startingPlayer.SessionMinutes = teamStartResult.SessionWindow.LengthInMinutes;
        startingPlayer.SessionBegin = teamStartResult.SessionWindow.Start;
        startingPlayer.SessionEnd = teamStartResult.SessionWindow.End;

        var asViewModel = _mapper.Map<Player>(startingPlayer);
        await _hubBus.SendTeamSessionStarted(asViewModel, actor);

        return asViewModel;
    }

    public async Task<Player[]> List(PlayerDataFilter model, bool sudo = false)
    {
        if (!sudo && !model.WantsGame && !model.WantsTeam)
            return [];

        var q = BuildListQuery(model);
        var players = await _mapper.ProjectTo<Player>(q).ToArrayAsync();
        var queriedTeamIds = players.Select(p => p.TeamId).ToArray();

        var teamSponsors = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Sponsor)
            .Where(p => queriedTeamIds.Contains(p.TeamId))
            .Select(p => new
            {
                p.TeamId,
                SponsorLogoFileNames = p.Sponsor.Logo
            })
            .GroupBy(g => g.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(thing => thing.SponsorLogoFileNames).ToArray());

        foreach (var player in players)
            if (player.TeamId.IsNotEmpty() && teamSponsors.ContainsKey(player.TeamId))
                player.TeamSponsorLogos = teamSponsors[player.TeamId];
            else
                player.TeamSponsorLogos = Array.Empty<string>();

        return players;
    }

    public async Task<Standing[]> Standings(PlayerDataFilter model)
    {
        if (model.gid.IsEmpty())
            return [];

        model.Filter = [.. model.Filter, PlayerDataFilter.FilterScoredOnly];
        model.mode = PlayerMode.Competition.ToString();
        var q = BuildListQuery(model);
        var standings = await _mapper.ProjectTo<Standing>(q).ToArrayAsync();

        // as a temporary workaround until we get the new scoreboard, we need to manually 
        // set the Sponsors property to accommodate multisponsor teams.
        var allTeamIds = standings.Select(s => s.TeamId);
        var allSponsors = await _store.WithNoTracking<Data.Sponsor>()
            .ToDictionaryAsync(s => s.Id, s => s);

        var teamsWithSponsors = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => allTeamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(p => p.SponsorId).ToArray());

        foreach (var standing in standings)
        {
            var distinctSponsors = teamsWithSponsors[standing.TeamId].Distinct().Select(s => allSponsors[s]);
            standing.TeamSponsors = _mapper.Map<Sponsor[]>(distinctSponsors);
        }
        return standings;
    }

    private IQueryable<Data.Player> BuildListQuery(PlayerDataFilter model)
    {
        var ts = _now.Get();

        var q = _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.User)
            .Include(p => p.Sponsor)
            .Include(p => p.AdvancedFromGame)
            .AsNoTracking();

        if (model.WantsMode)
            q = q.Where(p => p.Mode == Enum.Parse<PlayerMode>(model.mode, true));

        if (model.WantsGame)
        {
            q = q.Where(p => p.GameId == model.gid);

            if (model.WantsOrg)
                q = q.Where(p => p.SponsorId == model.org);
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
            q = q.WhereDateIsNotEmpty(p => p.SessionEnd);

        if (model.WantsAdvanced)
            q = q.Where(p => p.Advanced);

        if (model.WantsDismissed)
            q = q.Where(p => !p.Advanced);

        if (model.WantsPending)
            q = q.Where(p => p.Name != null && p.Name != string.Empty && p.Name != p.ApprovedName);

        if (model.WantsDisallowed)
            q = q.Where(u => !string.IsNullOrEmpty(u.NameStatus));

        if (model.WantsScored)
            q = q.WhereIsScoringPlayer();

        if (model.Term.NotEmpty())
        {
            var term = model.Term.ToLower();

            q = q.Where
            (
                p =>
                    p.ApprovedName.ToLower().Contains(term) ||
                    p.Name.ToLower().Contains(term) ||
                    p.Id.StartsWith(term) ||
                    p.TeamId.StartsWith(term) ||
                    p.UserId.StartsWith(term) ||
                    p.Sponsor.Name.StartsWith(term) ||
                    p.User.Name.ToLower().Contains(term) ||
                    p.User.ApprovedName.ToLower().Contains(term) ||
                    _store.WithNoTracking<Data.Player>().Where(p2 => p2.TeamId == p.TeamId && (p2.UserId.StartsWith(term) || p2.User.ApprovedName.ToLower().Contains(term))).Any()
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
        var result = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Game)
                .ThenInclude(g => g.Specs)
            .Include(p => p.Game)
                .ThenInclude(g => g.Prerequisites)
            .Include(p => p.Challenges)
                .ThenInclude(c => c.Events)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (result.Game.AllowTeam)
        {
            result.Challenges = await _store
                .WithNoTracking<Data.Challenge>()
                .Include(c => c.Events)
                .Where(c => c.TeamId == result.TeamId)
                .ToArrayAsync();
        }

        var mapped = _mapper.Map<BoardPlayer>(result);
        mapped.ChallengeDocUrl = CoreOptions.ChallengeDocUrl;

        // handle relative urls in challenge text
        if (mapped.Challenges is not null)
        {
            foreach (var challenge in mapped.Challenges)
            {
                challenge.State = ChallengeService.TransformStateRelativeUrls(challenge.State);
            }
        }

        return mapped;
    }

    public async Task<TeamInvitation> GenerateInvitation(string id)
    {
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Select(p => new
            {
                p.Id,
                p.Role
            })
            .SingleAsync(p => p.Id == id);

        if (player.Role != PlayerRole.Manager)
            throw new ActionForbidden();

        byte[] buffer = new byte[16];

        new Random().NextBytes(buffer);

        var code = Convert.ToBase64String(buffer)
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty);

        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.InviteCode, code));

        return new TeamInvitation { Code = code };
    }

    public async Task<Player> Enlist(PlayerEnlistment model, User actor, CancellationToken cancellationToken)
    {
        var canIgnoreRegistrationWindow = await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow);

        var player = await _store
            .WithTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .SingleOrDefaultAsync(p => p.Id == model.PlayerId, cancellationToken) ?? throw new ResourceNotFound<Data.Player>(model.PlayerId);

        if (player.SponsorId.IsEmpty() || player.Sponsor is null)
            throw new PlayerHasDefaultSponsor(model.PlayerId);

        var playersWithThisCode = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Game)
            .Where(p => p.InviteCode == model.Code)
            .ToArrayAsync(cancellationToken);

        var teamIds = playersWithThisCode.Select(p => p.TeamId).Distinct().ToArray();
        if (teamIds.Length != 1)
            throw new CantResolveTeamFromCode(model.Code, teamIds);

        var manager = _teamService.ResolveCaptain(playersWithThisCode);

        if (player.GameId != manager.GameId)
            throw new NotYetRegistered(player.Id, manager.GameId);

        if (player.Id == manager.Id)
            return _mapper.Map<Player>(player);

        var game = await _store.SingleAsync<Data.Game>(manager.GameId, cancellationToken);

        if (!canIgnoreRegistrationWindow && !game.RegistrationActive)
            throw new RegistrationIsClosed(manager.GameId);

        if (!canIgnoreRegistrationWindow && manager.SessionBegin.Year > 1)
            throw new RegistrationIsClosed(manager.GameId, "Registration begins in more than a year.");

        if (!canIgnoreRegistrationWindow && manager.Game.RequireSponsoredTeam && manager.SponsorId != player.SponsorId)
            throw new RequiresSameSponsor(manager.GameId, manager.Id, manager.Sponsor.Name, player.Id, player.Sponsor.Name);

        var count = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == manager.TeamId)
            .CountAsync(cancellationToken);

        if (manager.Game.AllowTeam && count >= manager.Game.MaxTeamSize)
            throw new TeamIsFull(manager.Id, count, manager.Game.MaxTeamSize);

        player.TeamId = manager.TeamId;
        player.Role = PlayerRole.Member;
        player.InviteCode = model.Code;

        await _store.SaveUpdate(player, cancellationToken);

        var mappedPlayer = _mapper.Map<Player>(player);
        await _hubBus.SendPlayerEnrolled(mappedPlayer, actor);
        await _mediator.Publish(new GameEnrolledPlayersChangeNotification(new GameEnrolledPlayersChangeContext(player.GameId, game.RequireSynchronizedStart)), cancellationToken);

        var isSyncStartGame = await _store.WithNoTracking<Data.Game>().Where(g => g.Id == mappedPlayer.GameId && g.RequireSynchronizedStart).AnyAsync();
        if (isSyncStartGame)
            await _syncStartGameService.HandleSyncStartStateChanged(mappedPlayer.GameId, cancellationToken);

        return mappedPlayer;
    }

    public async Task Unenroll(PlayerUnenrollRequest request, CancellationToken cancellationToken)
    {
        // make sure we've got a real player
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Game)
            .SingleAsync(p => p.Id == request.PlayerId, cancellationToken);

        // record sync start state because we need to raise events after we're done if the game is sync start
        var gameIsSyncStart = player.Game.RequireSynchronizedStart;

        // archive challenges and delete the player
        await ChallengeService.ArchivePlayerChallenges(player);

        // delete the player record
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == request.PlayerId)
            .ExecuteDeleteAsync(cancellationToken);

        // notify listeners on SignalR (like the team)
        var playerModel = _mapper.Map<Player>(player);
        await _hubBus.SendPlayerLeft(playerModel, request.Actor);
        await _mediator.Publish(new GameEnrolledPlayersChangeNotification(new GameEnrolledPlayersChangeContext(player.GameId, gameIsSyncStart)), cancellationToken);

        // update sync start if needed
        if (gameIsSyncStart)
            await _syncStartGameService.HandleSyncStartStateChanged(playerModel.GameId, cancellationToken);
    }

    public async Task<TeamChallenge[]> LoadChallengesForTeam(string teamId)
    {
        return await _mapper.ProjectTo<TeamChallenge>(_store.WithNoTracking<Data.Challenge>().Where(c => c.TeamId == teamId)).ToArrayAsync();
    }

    public async Task<TeamSummary[]> LoadGameTeamsMailMetadata(string gameId)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.GameId == gameId)
            .ToArrayAsync();

        var teams = players
            .GroupBy(p => p.TeamId)
            .Select(g => new TeamSummary
            {
                Id = g.Key,
                Name = g.First().ApprovedName,
                Sponsor = g.First().Sponsor.Logo,
                Members = g.Select(i => i.UserId).ToArray()
            })
            .ToArray()
        ;

        return teams;
    }

    public async Task<IEnumerable<ObserveTeam>> ObserveTeams(string id)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
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
            .Select(c => new ObserveTeam
            {
                TeamId = c.TeamId,
                ApprovedName = c.ApprovedName,
                Sponsors = _mapper.Map<Sponsor[]>(players.Where(p => p.TeamId == c.TeamId).Select(p => p.Sponsor)),
                GameId = c.GameId,
                SessionBegin = c.SessionBegin,
                SessionEnd = c.SessionEnd,
                Rank = c.Rank,
                Score = c.Score,
                Time = c.Time,
                CorrectCount = c.CorrectCount,
                PartialCount = c.PartialCount,
                Advanced = c.Advanced,
                Members = players.Where(p => p.TeamId == c.TeamId).Select(i => new ObserveTeamPlayer
                {
                    Id = i.UserId,
                    ApprovedName = i.User.ApprovedName,
                    Role = i.Role,
                    UserId = i.UserId
                }).OrderBy(p => p.ApprovedName).ToArray()
            })
            .OrderBy(c => c.ApprovedName)
            .ToArray();

        return teams;
    }

    public async Task AdvanceTeams(TeamAdvancement model)
    {
        var teams = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == model.GameId)
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.Name,
                p.NameStatus,
                p.Role,
                p.SponsorId,
                p.TeamId,
                p.UserId,
            })
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(kv => kv.Key, kv => kv.ToArray());

        var enrollments = new List<Data.Player>();

        foreach (var team in teams)
        {
            string newId = _guids.GetGuid();
            // compute complete score, including bonuses
            var teamScore = await _scores.GetTeamScore(team.Key, CancellationToken.None);

            foreach (var player in team.Value)
            {
                var newPlayer = new Data.Player
                {
                    TeamId = newId,
                    UserId = player.UserId,
                    GameId = model.NextGameId,
                    AdvancedFromGameId = model.GameId,
                    AdvancedFromPlayerId = player.Id,
                    AdvancedFromTeamId = player.TeamId,
                    AdvancedWithScore = model.WithScores ? teamScore.OverallScore.TotalScore : null,
                    ApprovedName = player.ApprovedName,
                    Name = player.Name,
                    NameStatus = player.NameStatus,
                    SponsorId = player.SponsorId,
                    Role = player.Role,
                    Score = model.WithScores ? (int)Math.Floor(teamScore.OverallScore.TotalScore) : 0,
                    WhenCreated = _now.Get()
                };

                enrollments.Add(newPlayer);
            }
        }

        await _store.SaveAddRange(enrollments.ToArray());

        var allAdvancingPlayerIds = teams.Values
            .SelectMany(v => v.Select(p => p.Id))
            .Distinct()
            .ToArray();

        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => allAdvancingPlayerIds.Contains(p.Id))
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.Advanced, true));


        // for now, this is a little goofy, but raising any team's score change will rerank the game,
        // and that's what we want, so...
        if (teams.Count > 0)
            await _mediator.Publish(new ScoreChangedNotification(model.TeamIds.First()));
    }

    public async Task<PlayerCertificate> MakeCertificate(string id)
    {
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Game)
            .Include(p => p.User)
                .ThenInclude(u => u.PublishedCompetitiveCertificates)
            .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .FirstOrDefaultAsync(p => p.Id == id);

        var playerCount = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == player.GameId && p.SessionEnd > DateTimeOffset.MinValue)
            .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .CountAsync();

        var teamCount = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == player.GameId &&
                p.SessionEnd > DateTimeOffset.MinValue)
            .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .GroupBy(p => p.TeamId)
            .CountAsync();

        return CertificateFromTemplate(player, playerCount, teamCount);
    }

    public async Task<IEnumerable<PlayerCertificate>> MakeCertificates(string uid)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var completedSessions = await _store
            .WithNoTracking<Data.Player>()
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
            .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .WhereIsScoringPlayer()
            .OrderByDescending(p => p.Game.GameEnd)
            .ToArrayAsync();

        return completedSessions.Select
        (
            c => CertificateFromTemplate
            (
                c,
                _store.WithNoTracking<Data.Player>()
                    .Where(p => p.Game == c.Game && p.SessionEnd > DateTimeOffset.MinValue)
                    .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
                    .WhereIsScoringPlayer()
                    .Count(),
                _store.WithNoTracking<Data.Player>()
                    .Where(p => p.Game == c.Game && p.SessionEnd > DateTimeOffset.MinValue)
                    .WhereIsScoringPlayer()
                    .Where(p => p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
                    .GroupBy(p => p.TeamId).Count()
            )
        ).ToArray();
    }

    private PlayerCertificate CertificateFromTemplate(Data.Player player, int playerCount, int teamCount)
    {
        var certificateHTML = player.Game.CertificateTemplate;
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
            Game = _mapper.Map<Game>(player.Game),
            PublishedOn = player.User.PublishedCompetitiveCertificates.FirstOrDefault(c => c.GameId == player.Game.Id)?.PublishedOn,
            Player = _mapper.Map<Player>(player),
            Html = certificateHTML
        };
    }

    private async Task<Player> RegisterPracticeSession(NewPlayer model, Data.User user, CancellationToken cancellationToken)
    {
        // load practice settings
        var settings = await _practiceService.GetSettings(cancellationToken);

        // check for existing sessions
        var nowStamp = _now.Get();

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where
            (
                p =>
                    p.UserId == model.UserId &&
                    p.Mode == PlayerMode.Practice &&
                    p.SessionEnd > nowStamp
            ).ToArrayAsync(cancellationToken);

        if (players.Any(p => p.GameId == model.GameId))
            return _mapper.Map<Player>(players.First(p => p.GameId == model.GameId));

        // find gamespaces across all practice sessions
        var teamIds = players.Select(p => p.TeamId).ToArray();
        var game = await _store.SingleAsync<Data.Game>(model.GameId, cancellationToken);
        foreach (var teamId in teamIds)
        {
            // practice mode only allows a single gamespace
            if (await _teamService.IsAtGamespaceLimit(teamId, game, cancellationToken))
                throw new UserLevelPracticeGamespaceLimitReached(model.UserId, model.GameId, teamIds);
        }

        // don't exceed global configured limit
        if (settings.MaxConcurrentPracticeSessions.HasValue)
        {
            int count = await _store
                .WithNoTracking<Data.Player>()
                .CountAsync<Data.Player>
                (
                    p =>
                        p.Mode == PlayerMode.Practice &&
                        p.SessionEnd > nowStamp, cancellationToken
                );

            if (count >= settings.MaxConcurrentPracticeSessions.Value)
            {
                _logger.LogWarning($"Can't start a new practice session. There are {count} active practice sessions, and the limit is {settings.MaxConcurrentPracticeSessions.Value}.");
                throw new PracticeSessionLimitReached();
            }
        }

        var entity = InitializePlayer(model, user, settings.DefaultPracticeSessionLengthMinutes);

        // start session
        entity.SessionBegin = nowStamp;
        entity.SessionEnd = entity.SessionBegin.AddMinutes(entity.SessionMinutes);
        entity.Mode = PlayerMode.Practice;

        await _store.Create(entity, cancellationToken);
        return _mapper.Map<Player>(entity);
    }

    private Data.Player InitializePlayer(NewPlayer model, Data.User user, int duration)
        => new()
        {
            ApprovedName = user.ApprovedName,
            GameId = model.GameId,
            Name = user.ApprovedName,
            Role = PlayerRole.Manager,
            SessionMinutes = duration,
            SponsorId = user.SponsorId,
            TeamId = GuidService.GetGuid(),
            UserId = model.UserId,
            WhenCreated = _now.Get()
        };
}
