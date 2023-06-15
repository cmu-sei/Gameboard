// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class TicketService : _Service
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IGuidService _guids;
        private readonly INowService _now;
        ITicketStore Store { get; }

        internal static char LABELS_DELIMITER = ' ';

        public TicketService(
            IFileUploadService fileUploadService,
            IGuidService guids,
            ILogger<TicketService> logger,
            IMapper mapper,
            INowService now,
            CoreOptions options,
            ITicketStore store
        ) : base(logger, mapper, options)
        {
            Store = store;
            _fileUploadService = fileUploadService;
            _guids = guids;
            _now = now;
        }

        public async Task<Ticket> Retrieve(string id)
        {
            var entity = await Store.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            return TransformInPlace(Mapper.Map<Ticket>(entity));
        }

        public async Task<Ticket> Retrieve(int id)
        {
            var entity = await Store.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            return TransformInPlace(Mapper.Map<Ticket>(entity));
        }

        public async Task<Ticket> Create(NewTicket model, string actorId, bool sudo)
        {
            Data.Ticket entity;
            var timestamp = _now.Get();

            if (sudo) // staff with full management capability
            {
                entity = Mapper.Map<Data.Ticket>(model);
                AddActivity(entity, actorId, !entity.Status.IsEmpty(), !entity.AssigneeId.IsEmpty(), timestamp);
                entity.StaffCreated = true;
            }
            else
            {
                var selfMade = Mapper.Map<SelfNewTicket>(model);
                entity = Mapper.Map<Data.Ticket>(selfMade);
                entity.StaffCreated = false;
            }

            if (entity.RequesterId.IsEmpty())
                entity.RequesterId = actorId;
            if (entity.Status.IsEmpty())
                entity.Status = "Open";

            if (!entity.PlayerId.IsEmpty() || !entity.ChallengeId.IsEmpty())
            {
                await UpdatedSessionContext(entity);
            }

            entity.CreatorId = actorId;
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

            await Store.Create(entity);
            return TransformInPlace(Mapper.Map<Ticket>(entity));
        }

        public async Task<Ticket> Update(ChangedTicket model, string actorId, bool sudo)
        {
            var entity = await Store.Retrieve(model.Id);
            var timestamp = _now.Get();

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
            }
            else // regular participant can only edit a few fields
            {

                Mapper.Map(
                    Mapper.Map<SelfChangedTicket>(model),
                    entity
                );
            }

            entity.LastUpdated = timestamp;

            await Store.Update(entity);
            return TransformInPlace(Mapper.Map<Ticket>(entity));
        }

        public async Task<IEnumerable<TicketSummary>> List(TicketSearchFilter model, string userId, bool sudo)
        {
            var q = Store.List(model.Term);

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
                var userTeams = await Store.DbContext.Players
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

            return Transform(await Mapper.ProjectTo<TicketSummary>(q).ToArrayAsync());
        }

        public async Task<TicketActivity> AddComment(NewTicketComment model, string actorId)
        {
            var entity = await Store.Load(model.TicketId);
            var timestamp = _now.Get();
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
            await Store.Update(entity);

            var result = Mapper.Map<TicketActivity>(commentActivity);
            result.RequesterId = entity.RequesterId;
            result.LastUpdated = entity.LastUpdated;
            result.Key = entity.Key;
            result.Status = entity.Status;

            return result;
        }

        public async Task<string[]> ListLabels(SearchFilter model)
        {
            var q = Store.List(model.Term);
            var tickets = await Mapper.ProjectTo<TicketSummary>(q).ToArrayAsync();

            var b = tickets
                .Where(t => !t.Label.IsEmpty())
                .SelectMany(t => TransformTicketLabels(t.Label))
                .OrderBy(t => t)
                .ToHashSet().ToArray();

            return b;
        }

        public async Task<bool> UserIsEnrolled(string gameId, string userId)
        {
            return await Store.DbContext.Users.AnyAsync(u =>
                u.Id == userId &&
                u.Enrollments.Any(e => e.GameId == gameId)
            );
        }

        public async Task<bool> IsOwnerOrTeamMember(int ticketId, string userId)
        {
            var ticket = await Store.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            if (ticket.TeamId.IsEmpty())
                return false;

            // if team associated with ticket, see if this user has an enrollment with matching teamId
            return await Store.DbContext.Players.AnyAsync(p =>
                p.UserId == userId &&
                p.TeamId == ticket.TeamId
            );
        }

        public async Task<bool> IsOwnerOrTeamMember(string ticketId, string userId)
        {
            var ticket = await Store.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            if (ticket.TeamId.IsEmpty())
                return false;

            // if team associated with ticket, see if this user has an enrollment with matching teamId
            return await Store.DbContext.Players.AnyAsync(p =>
                p.UserId == userId &&
                p.TeamId == ticket.TeamId
            );
        }

        public async Task<bool> IsOwner(string ticketId, string userId)
        {
            var ticket = await Store.Load(ticketId);
            if (ticket == null)
                return false;
            if (ticket.RequesterId == userId)
                return true;
            return false;
        }

        public async Task<bool> UserCanUpdate(string ticketId, string userId)
        {
            var ticket = await Store.Load(ticketId);
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

        internal string TransformTicketKey(int key)
        {
            return Options.KeyPrefix + "-" + key.ToString();
        }

        private async Task UpdatedSessionContext(Data.Ticket entity)
        {
            if (!entity.ChallengeId.IsEmpty())
            {
                var challenge = await Store.DbContext.Challenges.FirstOrDefaultAsync(c => c.Id == entity.ChallengeId);
                if (challenge != null)
                {
                    entity.TeamId = challenge.TeamId;
                    entity.PlayerId = challenge.PlayerId;
                    return;
                }
            }
            else if (!entity.PlayerId.IsEmpty())
            {
                var player = await Store.DbContext.Players.FirstOrDefaultAsync(c =>
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
                    Id = Guid.NewGuid().ToString("n"),
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
                    Id = Guid.NewGuid().ToString("n"),
                    UserId = actorId,
                    AssigneeId = entity.AssigneeId,
                    Type = ActivityType.AssigneeChange,
                    Timestamp = timestamp
                };
                entity.Activity.Add(assigneeActivity);
            }
        }

        private IEnumerable<TicketSummary> Transform(IEnumerable<TicketSummary> tickets)
        {
            return tickets.Select(t =>
            {
                t.FullKey = FullKey(t.Key);
                return t;
            }).ToArray();
        }

        private Ticket TransformInPlace(Ticket ticket)
        {
            ticket.FullKey = TransformTicketKey(ticket.Key);
            return ticket;
        }

        private string FullKey(int key)
        {
            return Options.KeyPrefix + "-" + key.ToString();
        }
    }
}
