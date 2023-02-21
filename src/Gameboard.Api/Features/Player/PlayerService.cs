// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.UnityGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services
{
    public class PlayerService
    {
        CoreOptions CoreOptions { get; }
        ChallengeService ChallengeService { get; set; }
        IPlayerStore Store { get; }
        IGameStore GameStore { get; }
        IInternalHubBus HubBus { get; }
        ITeamService TeamService { get; }
        IUserStore UserStore { get; }
        IMapper Mapper { get; }
        IMemoryCache LocalCache { get; }
        TimeSpan _idmapExpiration = new TimeSpan(0, 30, 0);
        ITopoMojoApiClient Mojo { get; }
        private IUnityGameService UnityGameService { get; }

        public PlayerService(
            CoreOptions coreOptions,
            ChallengeService challengeService,
            IPlayerStore store,
            IUserStore userStore,
            IGameStore gameStore,
            IInternalHubBus hubBus,
            ITeamService teamService,
            IMapper mapper,
            IMemoryCache localCache,
            ITopoMojoApiClient mojo,
            IUnityGameService unityGameService
        )
        {
            CoreOptions = coreOptions;
            ChallengeService = challengeService;
            HubBus = hubBus;
            Store = store;
            GameStore = gameStore;
            TeamService = teamService;
            UserStore = userStore;
            Mapper = mapper;
            LocalCache = localCache;
            Mojo = mojo;
            UnityGameService = unityGameService;
        }

        public async Task<Player> Enroll(NewPlayer model, User actor)
        {
            var game = await GameStore.Retrieve(model.GameId);

            if (!actor.IsRegistrar && !game.RegistrationActive)
                throw new RegistrationIsClosed(model.GameId);

            var user = await Store.GetUserEnrollments(model.UserId);
            if (user.Enrollments.Any(p => p.GameId == model.GameId))
                throw new AlreadyRegistered(model.UserId, model.GameId);

            var entity = Mapper.Map<Data.Player>(model);

            entity.TeamId = Guid.NewGuid().ToString("n");
            entity.Role = PlayerRole.Manager;
            entity.ApprovedName = user.ApprovedName;
            entity.Name = user.ApprovedName;
            entity.Sponsor = user.Sponsor;
            entity.SessionMinutes = game.SessionMinutes;

            await Store.Create(entity);
            await HubBus.SendPlayerEnrolled(Mapper.Map<Api.Player>(entity), actor);

            return Mapper.Map<Player>(entity);
        }

        /// <summary>
        /// Maps a PlayerId to it's UserId
        /// </summary>
        /// <remarks>This happens frequently for authorization, so cache the mapping.</remarks>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<string> MapId(string playerId)
        {
            if (LocalCache.TryGetValue(playerId, out string userId))
                return userId;

            userId = (await Store.Retrieve(playerId))?.UserId;
            LocalCache.Set(playerId, userId, _idmapExpiration);

            return userId;
        }

        public async Task<Player> Retrieve(string id)
        {
            return Mapper.Map<Player>(await Store.Retrieve(id));
        }

        public async Task<Player> Update(ChangedPlayer model, User actor, bool sudo = false)
        {
            var entity = await Store.Retrieve(model.Id);
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
                bool found = await Store.DbSet.AnyAsync(p =>
                    p.GameId == entity.GameId &&
                    p.TeamId != entity.TeamId &&
                    p.Name == entity.Name
                );

                if (found)
                    entity.NameStatus = AppConstants.NameStatusNotUnique;
            }

            await Store.Update(entity);
            await HubBus.SendTeamUpdated(Mapper.Map<Api.Player>(entity), actor);
            return Mapper.Map<Player>(entity);
        }

        public async Task<Player> ResetSession(SessionResetRequest request)
        {
            var player = await Store
                .DbSet
                .AsNoTracking()
                .Include(p => p.Game)
                .SingleAsync(p => p.Id == request.PlayerId);

            // unlike unenroll, we archive the entire team's challenges
            await ChallengeService.ArchiveTeamChallenges(player.TeamId);

            // delete the entire team (this is the primary difference from "unenroll")
            await Store.DeleteTeam(player.TeamId);

            // notify hub that the team is deleted /players left so the client can respond
            var playerModel = Mapper.Map<Player>(player);
            await HubBus.SendPlayerLeft(playerModel, request.Actor);
            await HubBus.SendTeamDeleted(playerModel, request.Actor);

            // try undeploy unity if necessary
            if (UnityGameService.IsUnityGame(player.Game))
                await UnityGameService.UndeployGame(player.GameId, player.TeamId);

            return playerModel;
        }

        public async Task<Player> StartSession(SessionStartRequest model, User actor, bool sudo)

        {
            var team = await Store.ListTeamByPlayer(model.PlayerId);

            var player = team.First();
            var game = await Store.DbContext.Games.FindAsync(player.GameId);

            if (!sudo && game.SessionLimit > 0)
            {
                var ts = DateTimeOffset.UtcNow;

                int sessionCount = await Store.DbSet
                    .CountAsync(p =>
                        p.GameId == game.Id &&
                        p.Role == PlayerRole.Manager &&
                        ts < p.SessionEnd
                    );

                if (sessionCount >= game.SessionLimit)
                    throw new SessionLimitReached();
            }

            if (!sudo && game.IsLive.Equals(false))
                throw new GameNotActive();

            if (
                !sudo &&
                game.RequireTeam &&
                team.Length < game.MinTeamSize
            )
                throw new InvalidTeamSize();

            var st = DateTimeOffset.UtcNow;
            var et = st.AddMinutes(game.SessionMinutes);

            foreach (var p in team)
            {
                p.SessionMinutes = game.SessionMinutes;
                p.SessionBegin = st;
                p.SessionEnd = et;
            }

            await Store.Update(team);

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

                Store.DbContext.Add(challenge);
                await Store.DbContext.SaveChangesAsync();
            }

            var asViewModel = Mapper.Map<Api.Player>(player);
            await HubBus.SendTeamStarted(asViewModel, actor);

            return asViewModel;
        }

        public async Task<Player> ExtendSession(SessionChangeRequest model, User actor)
        {
            var team = await Store.ListTeam(model.TeamId);

            if (team.First().IsLive.Equals(false))
                throw new SessionNotActive(team.First().Id);

            if (team.First().SessionEnd >= model.SessionEnd)
                throw new InvalidExtendSessionRequest(team.First().SessionEnd, model.SessionEnd);

            foreach (var player in team)
                player.SessionEnd = model.SessionEnd;

            await Store.Update(team);

            // push gamespace extension
            var challenges = await Store.DbContext.Challenges
                .Where(c => c.TeamId == team.First().TeamId)
                .Select(c => c.Id)
                .ToArrayAsync();

            foreach (string id in challenges)
                await Mojo.UpdateGamespaceAsync(new ChangedGamespace
                {
                    Id = id,
                    ExpirationTime = model.SessionEnd
                });

            var captain = await TeamService.ResolveCaptain(model.TeamId);
            await HubBus.SendTeamUpdated(Mapper.Map<Player>(captain), actor);

            return Mapper.Map<Player>(
                team.FirstOrDefault(p =>
                    p.Role == PlayerRole.Manager
                )
            );
        }

        public async Task<Player[]> List(PlayerDataFilter model, bool sudo = false)
        {
            if (!sudo && !model.WantsGame && !model.WantsTeam)
                return new Player[] { };

            var q = _List(model);

            return await Mapper.ProjectTo<Player>(q).ToArrayAsync();
        }

        public async Task<Standing[]> Standings(PlayerDataFilter model)
        {
            if (model.gid.IsEmpty())
                return new Standing[] { };

            model.Filter = model.Filter
                .Append(PlayerDataFilter.FilterScoredOnly)
                .ToArray()
            ;

            var q = _List(model);

            return await Mapper.ProjectTo<Standing>(q).ToArrayAsync();
        }

        private IQueryable<Data.Player> _List(PlayerDataFilter model)
        {
            var ts = DateTimeOffset.UtcNow;

            var q = Store.List()
                .Include(p => p.User)
                .AsNoTracking();

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
                q = q.Where(p => p.Score > 0);

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
                    Store.DbSet.Where(p2 => p2.TeamId == p.TeamId && (p2.UserId.StartsWith(term) || p2.User.ApprovedName.ToLower().Contains(term))).Any()
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
                await Store.LoadBoard(id)
            );

            mapped.ChallengeDocUrl = CoreOptions.ChallengeDocUrl;
            return mapped;
        }

        public async Task<TeamInvitation> GenerateInvitation(string id)
        {
            var player = await Store.Retrieve(id);

            if (player.Role != PlayerRole.Manager)
                throw new ActionForbidden();

            byte[] buffer = new byte[16];

            new Random().NextBytes(buffer);

            player.InviteCode = Convert.ToBase64String(buffer)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
            ;

            await Store.Update(player);

            return new TeamInvitation
            {
                Code = player.InviteCode
            };
        }

        public async Task<Player> Enlist(PlayerEnlistment model, User actor)
        {
            var sudo = actor.IsRegistrar;
            var manager = await Store.List()
                .Include(p => p.Game)
                .FirstOrDefaultAsync(
                    p => p.InviteCode == model.Code
                );

            var player = await Store.DbSet.FirstOrDefaultAsync(p => p.Id == model.PlayerId);

            if (player == null)
            {
                throw new ResourceNotFound<Player>(model.PlayerId);
            }

            if (player.GameId != manager.GameId)
            {
                throw new NotYetRegistered(player.Id, manager.GameId);
            }

            if (manager is not Data.Player)
                throw new InvalidInvitationCode(model.Code, "Couldn't find the manager record.");

            if (player.Id == manager.Id)
                return Mapper.Map<Player>(player);

            if (!sudo && !manager.Game.RegistrationActive)
                throw new RegistrationIsClosed(manager.GameId);

            if (!sudo && manager.SessionBegin.Year > 1)
                throw new RegistrationIsClosed(manager.GameId, "Registration begins in more than a year.");

            if (!sudo && manager.Game.RequireSponsoredTeam && !manager.Sponsor.Equals(player.Sponsor))
                throw new RequiresSameSponsor(manager.GameId, manager.Id, manager.Sponsor, player.Id, player.Sponsor);

            int count = await Store.List().CountAsync(p => p.TeamId == manager.TeamId);

            if (!sudo && manager.Game.AllowTeam && count >= manager.Game.MaxTeamSize)
                throw new TeamIsFull(manager.Id, count, manager.Game.MaxTeamSize);

            player.TeamId = manager.TeamId;
            player.Role = PlayerRole.Member;
            player.InviteCode = model.Code;

            await Store.Update(player);

            if (manager.Game.AllowTeam && !manager.Game.RequireSponsoredTeam)
                await UpdateTeamSponsors(manager.TeamId);

            var mappedPlayer = Mapper.Map<Player>(player);
            await HubBus.SendPlayerEnrolled(mappedPlayer, actor);
            return mappedPlayer;
        }

        public async Task Unenroll(PlayerUnenrollRequest request)
        {
            // they probably don't have challenge data on an unenroll, but in case an admin does this 
            // or something, we'll clean up their challenges
            var player = await Store.Retrieve(request.PlayerId);
            await ChallengeService.ArchivePlayerChallenges(player);

            // delete the player record
            await Store.Delete(request.PlayerId);

            // manage sponsor info about the team
            await UpdateTeamSponsors(player.TeamId);

            // notify listeners on SignalR (like the team)
            var playerModel = Mapper.Map<Player>(player);
            await HubBus.SendPlayerLeft(playerModel, request.Actor);
        }

        private async Task UpdateTeamSponsors(string teamId)
        {
            var members = await Store.DbSet
                .Where(p => p.TeamId == teamId)
                .Select(p => new
                {
                    Id = p.Id,
                    Sponsor = p.Sponsor,
                    IsManager = p.IsManager
                })
                .ToArrayAsync();

            if (members.Length == 0)
                return;

            var sponsors = string.Join('|', members
                .Select(p => p.Sponsor)
                .Distinct()
                .ToArray()
            );

            var manager = members.FirstOrDefault(p => p.IsManager);

            await Store
                .DbContext
                .Players
                .Where(p => p.Id == manager.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(p => p.TeamSponsors, sponsors));
        }

        public async Task<Team> LoadTeam(string id, bool sudo)
        {
            var players = await Store.ListTeam(id);
            if (players.Count() == 0)
                return null;

            var team = Mapper.Map<Team>(
                players.First(p => p.IsManager)
            );

            team.Members = Mapper.Map<TeamMember[]>(
                players.Select(p => p.User)
            );

            team.TeamSponsors = string.Join("|", players.Select(p => p.Sponsor));

            if (sudo)
                team.Challenges = Mapper.Map<TeamChallenge[]>(
                    await Store.ListTeamChallenges(id)
                );

            return team;
        }

        public async Task<TeamSummary[]> LoadTeams(string id, bool sudo)
        {
            var players = await Store.List()
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
            var players = await Store.List()
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
                    Sponsor = c.Sponsor,
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

            var allteams = await Store.List()
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
                            .Select(p => p.Sponsor)
                            .Distinct()
                            .ToArray()
                        );
                    }
                }
            }

            await Store.Create(enrollments);
            await Store.Update(allteams);
        }

        public async Task<PlayerCertificate> MakeCertificate(string id)
        {
            var player = await Store.List()
                .Include(p => p.Game)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            var playerCount = await Store.DbSet
                .Where(p => p.GameId == player.GameId &&
                    p.SessionEnd > DateTimeOffset.MinValue)
                .CountAsync();

            var teamCount = await Store.DbSet
                .Where(p => p.GameId == player.GameId &&
                    p.SessionEnd > DateTimeOffset.MinValue)
                .GroupBy(p => p.TeamId)
                .CountAsync();

            return CertificateFromTemplate(player, playerCount, teamCount);
        }

        public async Task<PlayerCertificate[]> MakeCertificates(string uid)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var completedSessions = await Store.List()
                .Include(p => p.Game)
                .Include(p => p.User)
                .Where(p => p.UserId == uid &&
                    p.SessionEnd > DateTimeOffset.MinValue &&
                    p.Game.GameEnd < now &&
                    p.Game.CertificateTemplate != null &&
                    p.Game.CertificateTemplate.Length > 0)
                .OrderByDescending(p => p.Game.GameEnd)
                .ToArrayAsync();

            return completedSessions.Select(c => CertificateFromTemplate(c,
                Store.DbSet
                    .Where(p => p.Game == c.Game &&
                        p.SessionEnd > DateTimeOffset.MinValue)
                    .Count(),
                Store.DbSet
                    .Where(p => p.Game == c.Game &&
                        p.SessionEnd > DateTimeOffset.MinValue)
                    .GroupBy(p => p.TeamId).Count()
            )).ToArray();
        }

        private Api.PlayerCertificate CertificateFromTemplate(Data.Player player, int playerCount, int teamCount)
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
                Player = Mapper.Map<Player>(player),
                Html = certificateHTML
            };
        }

        private Task ArchiveChallenges(IEnumerable<Challenge> challenges)
        {

            return Task.CompletedTask;
        }
    }
}
