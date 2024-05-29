// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Support;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Hubs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class TicketService : _Service
    {
        private readonly IActingUserService _actingUserService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IGuidService _guids;
        private readonly IMediator _mediator;
        private readonly INowService _now;
        private readonly IStore _store;
        private readonly ISupportHubBus _supportHubBus;
        private readonly ITeamService _teamService;
        ITicketStore TicketStore { get; }

        internal static char LABELS_DELIMITER = ' ';

        public TicketService(
            IActingUserService actingUserService,
            IFileUploadService fileUploadService,
            IGuidService guids,
            ILogger<TicketService> logger,
            IMapper mapper,
            IMediator mediator,
            INowService now,
            CoreOptions options,
            IStore store,
            ITicketStore ticketStore,
            ISupportHubBus supportHubBus,
            ITeamService teamService
        ) : base(logger, mapper, options)
        {
            TicketStore = ticketStore;
            _actingUserService = actingUserService;
            _fileUploadService = fileUploadService;
            _guids = guids;
            _mediator = mediator;
            _now = now;
            _store = store;
            _supportHubBus = supportHubBus;
            _teamService = teamService;
        }

        public string GetFullKey(int key)
            => $"{(Options.KeyPrefix.IsEmpty() ? "GB" : Options.KeyPrefix)}-{key}";

        public async Task<Ticket> Retrieve(string id)
        {
            var entity = await TicketStore.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            var ticket = TransformInPlace(Mapper.Map<Ticket>(entity));

            if (entity.PlayerId.IsNotEmpty() && entity.TeamId.IsNotEmpty())
            {
                var team = await _teamService.GetTeam(entity.TeamId);
                ticket.TeamName = team.ApprovedName;
            }

            if (entity.Challenge is not null)
            {
                ticket.IsTeamGame = ticket.Challenge.AllowTeam;
            }

            return ticket;
        }

        public async Task<Ticket> Retrieve(int id)
        {
            var entity = await TicketStore.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            var ticket = TransformInPlace(Mapper.Map<Ticket>(entity));

            if (entity.PlayerId.IsNotEmpty() && entity.TeamId.IsNotEmpty())
            {
                var team = await _teamService.GetTeam(entity.TeamId);
                ticket.TeamName = team.ApprovedName;
            }

            if (entity.Challenge is not null)
            {
                ticket.IsTeamGame = ticket.Challenge.AllowTeam;
            }

            return ticket;
        }

        public async Task<Ticket> Create(NewTicket model)
        {
            Data.Ticket entity;
            var timestamp = _now.Get();
            var actingUser = _actingUserService.Get();

            if (actingUser.IsSupport) // staff with full management capability
            {
                entity = Mapper.Map<Data.Ticket>(model);
                AddActivity(entity, actingUser.Id, !entity.Status.IsEmpty(), !entity.AssigneeId.IsEmpty(), timestamp);
                entity.StaffCreated = true;
            }
            else
            {
                var selfMade = Mapper.Map<SelfNewTicket>(model);
                entity = Mapper.Map<Data.Ticket>(selfMade);
                entity.StaffCreated = false;
            }

            if (entity.RequesterId.IsEmpty())
                entity.RequesterId = actingUser.Id;
            if (entity.Status.IsEmpty())
                entity.Status = "Open";

            if (!entity.PlayerId.IsEmpty() || !entity.ChallengeId.IsEmpty())
                await UpdatedSessionContext(entity);

            entity.CreatorId = actingUser.Id;
            entity.Created = timestamp;
            entity.LastUpdated = timestamp;

            // generate the insertion guid now so we can use it for file uploads
            entity.Id = _guids.GetGuid();

            // upload files
            var uploads = await _fileUploadService.Upload(Path.Combine(Options.SupportUploadsFolder, entity.Id), model.Uploads);
            if (uploads.Any())
            {
                var fileNames = uploads.Select(x => x.FileName).ToArray();
                entity.Attachments = Mapper.Map<string>(fileNames);
            }

            await TicketStore.Create(entity);
            var createdTicketModel = TransformInPlace(Mapper.Map<Ticket>(entity));

            // at creation time, the entity doesn't have the full Creator user, but we add it to the returned value
            // a) because that's helpful, and b) because the support hub needs to know
            createdTicketModel.Creator = new TicketUser
            {
                Id = actingUser.Id,
                ApprovedName = actingUser.ApprovedName,
                IsSupportPersonnel = actingUser.IsAdmin || actingUser.IsSupport
            };

            // send app-level notification
            await _mediator.Publish(new TicketCreatedNotification
            {
                Key = entity.Key,
                FullKey = GetFullKey(entity.Key),
                Title = entity.Summary,
                Description = entity.Description,
                Creator = new SimpleEntity { Id = actingUser.Id, Name = actingUser.ApprovedName },
                Challenge = createdTicketModel.Challenge is null ? null : new SimpleEntity { Id = createdTicketModel.ChallengeId, Name = createdTicketModel.Challenge.Name }
            });

            // notify the signalR hub (sends browser notifications to support staff)
            await _supportHubBus.SendTicketCreated(createdTicketModel);

            return createdTicketModel;
        }

        public IQueryable<Data.Ticket> GetGameOpenTickets(string gameId)
        {
            return _store
                .WithNoTracking<Data.Ticket>()
                .Where(t => t.Challenge.GameId == gameId || t.Player.Challenges.Any(c => c.GameId == gameId))
                .Where(t => t.Status != "Closed");
        }

        public async Task<Ticket> Update(ChangedTicket model, string actorId, bool sudo)
        {
            // need the creator to send updates
            var entity = await TicketStore.Retrieve(model.Id, q => q.Include(t => t.Creator));
            var actingUser = _actingUserService.Get();
            var timestamp = _now.Get();
            var updateClosesTicket = false;
            var updatedBySupport = false;
            var updatedByUser = false;

            if (sudo) // staff with full management capability
            {
                var prev = Mapper.Map<Ticket>(entity);
                model.Label = model.Label?.Trim();
                Mapper.Map(model, entity);
                var statusChanged = prev.Status != entity.Status;
                var assigneeChanged = prev.AssigneeId != entity.AssigneeId;
                AddActivity(entity, actorId, statusChanged, assigneeChanged, timestamp);

                if (prev.PlayerId != entity.PlayerId || prev.ChallengeId != entity.ChallengeId)
                {
                    await UpdatedSessionContext(entity);
                }

                if (statusChanged && entity.Status == "Closed")
                    updateClosesTicket = true;

                updatedBySupport = true;
            }
            else // regular participant can only edit a few fields
            {
                Mapper.Map(
                    Mapper.Map<SelfChangedTicket>(model),
                    entity
                );

                updatedByUser = true;
            }

            entity.LastUpdated = timestamp;

            await TicketStore.Update(entity);
            var updatedTicketModel = TransformInPlace(Mapper.Map<Ticket>(entity));

            if (updateClosesTicket)
                await _supportHubBus.SendTicketClosed(updatedTicketModel, _actingUserService.Get());
            else
            {
                if (updatedBySupport)
                    await _supportHubBus.SendTicketUpdatedBySupport(updatedTicketModel, actingUser);

                if (updatedByUser)
                    await _supportHubBus.SendTicketUpdatedByUser(updatedTicketModel, actingUser);
            }

            return updatedTicketModel;
        }

        public async Task<IEnumerable<TicketSummary>> List(TicketSearchFilter model, string userId, bool sudo)
        {
            var q = TicketStore.List(model.Term);

            if (model.WantsOpen)
                q = q.Where(t => t.Status == "Open");
            if (model.WantsInProgress)
                q = q.Where(t => t.Status == "In Progress");
            if (model.WantsClosed)
                q = q.Where(t => t.Status == "Closed");
            if (model.WantsNotClosed)
                q = q.Where(t => t.Status != "Closed");

            if (model.WantsAssignedToMe)
                q = q.Where(t => t.AssigneeId == userId);
            if (model.WantsUnassigned)
                q = q.Where(t => t.AssigneeId == null || t.AssigneeId == "");

            if (!sudo) // normal user should only see "their" tickets (requester or team member)
            {
                var userTeams = await TicketStore.DbContext.Players
                    .Where(p => p.UserId == userId && p.TeamId != null && p.TeamId != "")
                    .Select(p => p.TeamId)
                    .ToListAsync();

                q = q.Where(t => t.RequesterId == userId ||
                    userTeams.Any(i => i == t.TeamId));
            }

            // Ordering in descending order
            if (model.WantsOrderingDesc)
            {
                if (model.WantsOrderingByKey)
                    q = q.OrderByDescending(t => t.Key);
                if (model.WantsOrderingBySummary)
                    q = q.OrderByDescending(t => t.Summary.ToLower());
                if (model.WantsOrderingByStatus)
                    q = q.OrderByDescending(t => t.Status);
                if (model.WantsOrderingByCreated)
                    q = q.OrderByDescending(t => t.Created);
                if (model.WantsOrderingByUpdated)
                    q = q.OrderByDescending(t => t.LastUpdated);
            }
            // Ordering in ascending order
            else
            {
                if (model.WantsOrderingByKey)
                    q = q.OrderBy(t => t.Key);
                if (model.WantsOrderingBySummary)
                    q = q.OrderBy(t => t.Summary.ToLower());
                if (model.WantsOrderingByStatus)
                    q = q.OrderBy(t => t.Status);
                if (model.WantsOrderingByCreated)
                    q = q.OrderBy(t => t.Created);
                if (model.WantsOrderingByUpdated)
                    q = q.OrderBy(t => t.LastUpdated);
            }

            // Default clause to order by created date descending if nothing is given - we should never hit this.
            if (!(model.WantsOrderingByKey || model.WantsOrderingBySummary || model.WantsOrderingByStatus || model.WantsOrderingByCreated || model.WantsOrderingByUpdated))
                q = q.OrderByDescending(t => t.Created);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            var results = await Mapper.ProjectTo<TicketSummary>(q).ToArrayAsync();

            // create full ticket keys for results
            results = results.Select(r =>
            {
                r.FullKey = GetFullKey(r.Key);
                return r;
            }).ToArray();

            // have to filter by label on the "client" (non-database) because they're just a space-delimited
            // string in storage
            if (model.WithAllLabels.IsNotEmpty())
            {
                var allRequiredLabels = model.WithAllLabels.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                results = results.Where(r =>
                {
                    // if the thing has no labels, it's not going to pass this check
                    if (r.Label.IsEmpty())
                        return false;

                    var splits = r.Label.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return allRequiredLabels.All(l => splits.Any(s => s == l));
                })
                .ToArray();
            }

            return results;
        }

        public async Task<TicketActivity> AddComment(NewTicketComment model, string actorId)
        {
            var entity = await TicketStore.Load(model.TicketId);
            var timestamp = _now.Get();
            var actingUser = _actingUserService.Get();

            var commentActivity = new Data.TicketActivity
            {
                Id = _guids.GetGuid(),
                UserId = actorId,
                Message = model.Message,
                Type = ActivityType.Comment,
                Timestamp = timestamp
            };

            var uploads = await _fileUploadService.Upload(Path.Combine(Options.SupportUploadsFolder, model.TicketId, commentActivity.Id), model.Uploads);
            if (uploads.Any())
            {
                commentActivity.Attachments = Mapper.Map<string>(uploads.Select(x => x.FileName).ToArray());
            }

            entity.Activity.Add(Mapper.Map<Data.TicketActivity>(commentActivity));
            entity.LastUpdated = timestamp;

            // Set the ticket status to be Open if it was closed before and someone leaves a new comment
            entity.Status = entity.Status == "Closed" ? "Open" : entity.Status;
            await TicketStore.Update(entity);

            var result = Mapper.Map<TicketActivity>(commentActivity);
            result.RequesterId = entity.RequesterId;
            result.LastUpdated = entity.LastUpdated;
            result.Key = entity.Key;
            result.Status = entity.Status;

            // send signalR/browser notifications for updates
            // (have to do some yuck to fill out the Ticket object)
            var creator = await _store.WithNoTracking<Data.User>().SingleAsync(u => u.Id == entity.CreatorId);
            entity.Creator = creator;

            var mappedTicket = Mapper.Map<Ticket>(entity);
            mappedTicket.FullKey = GetFullKey(entity.Key);

            if (actingUser.IsSupport || actingUser.IsAdmin)
                await _supportHubBus.SendTicketUpdatedBySupport(mappedTicket, actingUser);
            else
                await _supportHubBus.SendTicketUpdatedByUser(mappedTicket, actingUser);

            return result;
        }

        public async Task<string[]> ListLabels(SearchFilter model)
        {
            var q = TicketStore.List(model.Term);
            var tickets = await Mapper.ProjectTo<TicketSummary>(q).ToArrayAsync();

            var b = tickets
                .Where(t => !t.Label.IsEmpty())
                .SelectMany(t => TransformTicketLabels(t.Label))
                .OrderBy(t => t)
                .ToHashSet()
                .ToArray();

            return b;
        }

        public async Task<bool> UserIsEnrolled(string gameId, string userId)
        {
            return await TicketStore.DbContext.Users.AnyAsync(u =>
                u.Id == userId &&
                u.Enrollments.Any(e => e.GameId == gameId)
            );
        }

        public async Task<bool> IsOwnerOrTeamMember(int ticketId, string userId)
        {
            var ticket = await TicketStore.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            if (ticket.TeamId.IsEmpty())
                return false;

            // if team associated with ticket, see if this user has an enrollment with matching teamId
            return await TicketStore.DbContext.Players.AnyAsync(p =>
                p.UserId == userId &&
                p.TeamId == ticket.TeamId
            );
        }

        public async Task<bool> IsOwnerOrTeamMember(string ticketId, string userId)
        {
            var ticket = await TicketStore.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            if (ticket.TeamId.IsEmpty())
                return false;

            // if team associated with ticket, see if this user has an enrollment with matching teamId
            return await TicketStore.DbContext.Players.AnyAsync(p =>
                p.UserId == userId &&
                p.TeamId == ticket.TeamId
            );
        }

        public async Task<bool> IsOwner(string ticketId, string userId)
        {
            var ticket = await TicketStore.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            return false;
        }

        public async Task<bool> UserCanUpdate(string ticketId, string userId)
        {
            var ticket = await TicketStore.Load(ticketId);
            if (ticket == null)
                return false;

            var updateUntilTime = DateTimeOffset.UtcNow.Add(new TimeSpan(0, -5, 0));
            if (ticket.RequesterId == userId && ticket.Created > updateUntilTime)
                return true;
            return false;
        }

        internal IEnumerable<string> TransformTicketLabels(string labels)
        {
            if (labels.IsEmpty())
                return Array.Empty<string>();

            return labels.Split(LABELS_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
        }

        private async Task UpdatedSessionContext(Data.Ticket entity)
        {
            if (!entity.ChallengeId.IsEmpty())
            {
                var challenge = await TicketStore.DbContext.Challenges.FirstOrDefaultAsync(c => c.Id == entity.ChallengeId);
                if (challenge != null)
                {
                    entity.TeamId = challenge.TeamId;
                    entity.PlayerId = challenge.PlayerId;
                    entity.Label = "";

                    // auto-add the "practice-challenge" tag - should be there if this is a practice challenge
                    if (challenge.PlayerMode == PlayerMode.Practice)
                    {
                        if (!entity.Label.Split(LABELS_DELIMITER, StringSplitOptions.RemoveEmptyEntries).Contains("practice-challenge"))
                        {
                            entity.Label += entity.Label + " practice-challenge".Trim();
                        }
                    }

                    return;
                }
            }
            else if (!entity.PlayerId.IsEmpty())
            {
                var player = await TicketStore.DbContext.Players.FirstOrDefaultAsync(c =>
                    c.Id == entity.PlayerId
                );
                if (player != null)
                {
                    entity.TeamId = player.TeamId;
                    entity.ChallengeId = null;
                    return;
                }
            }

            entity.TeamId = null;
            entity.ChallengeId = null;
            entity.PlayerId = null;
        }

        private void AddActivity(Data.Ticket entity, string actorId, bool statusChanged, bool assigneeChanged, DateTimeOffset timestamp)
        {
            if (statusChanged)
            {
                var statusActivity = new Data.TicketActivity
                {
                    Id = _guids.GetGuid(),
                    UserId = actorId,
                    Status = entity.Status,
                    Type = ActivityType.StatusChange,
                    Timestamp = timestamp
                };
                entity.Activity.Add(statusActivity);
            }
            if (assigneeChanged)
            {
                var assigneeActivity = new Data.TicketActivity
                {
                    Id = _guids.GetGuid(),
                    UserId = actorId,
                    AssigneeId = entity.AssigneeId,
                    Type = ActivityType.AssigneeChange,
                    Timestamp = timestamp
                };
                entity.Activity.Add(assigneeActivity);
            }
        }

        private Ticket TransformInPlace(Ticket ticket)
        {
            ticket.FullKey = GetFullKey(ticket.Key);
            return ticket;
        }
    }
}
