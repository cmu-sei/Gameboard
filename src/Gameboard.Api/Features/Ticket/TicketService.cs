// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class TicketService : _Service, IApiKeyAuthenticationService
    {
        ITicketStore Store { get; }

        private IMemoryCache _localcache;

        public TicketService(
            ILogger<TicketService> logger,
            IMapper mapper,
            CoreOptions options,
            ITicketStore store,
            IMemoryCache localcache
        ) : base(logger, mapper, options)
        {
            Store = store;
            _localcache = localcache;
        }
 
        public async Task<Ticket> Retrieve(string id, string actorId)
        {
            var entity = await Store.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            return Transform(Mapper.Map<Ticket>(entity));
        }

        public async Task<Ticket> Retrieve(int id, string actorId)
        {
            var entity = await Store.LoadDetails(id);
            entity.Activity = entity.Activity.OrderByDescending(a => a.Timestamp).ToList();
            return Transform(Mapper.Map<Ticket>(entity));
        }

       
        public async Task<Ticket> Create(NewTicket model, string actorId, bool sudo, List<UploadFile> uploads)
        {
            Data.Ticket entity;
            var timestamp = DateTimeOffset.UtcNow;
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

            if (uploads.Count() > 0) {
                var filenames = uploads.Select(x => x.FileName).ToArray();
                 entity.Attachments = Mapper.Map<string>(filenames);
            }

            await Store.Create(entity);

            return Transform(Mapper.Map<Ticket>(entity));
        }

        public async Task<Ticket> Update(ChangedTicket model, string actorId, bool sudo)
        {
            var entity = await Store.Retrieve(model.Id);
            var timestamp = DateTimeOffset.UtcNow;
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
            
            return Transform(Mapper.Map<Ticket>(entity));
        }

        public async Task<TicketSummary[]> List(TicketSearchFilter model, string userId, bool sudo)
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
            if (model.WantsOrderingDesc) {
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
            else {
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

        public async Task<TicketActivity> AddComment(NewTicketComment model, string actorId, List<UploadFile> uploads)
        {
            var entity = await Store.Load(model.TicketId);
            var timestamp = DateTimeOffset.UtcNow;
            var commentActivity = new Data.TicketActivity
            {
                Id = Guid.NewGuid().ToString("n"),
                UserId = actorId,
                Message = model.Message,
                Type = ActivityType.Comment,
                Timestamp = timestamp
            };

            if (uploads.Count() > 0) {
                commentActivity.Attachments = Mapper.Map<string>(uploads.Select(x => x.FileName).ToArray());
            }

            entity.Activity.Add(Mapper.Map<Data.TicketActivity>(commentActivity));
            entity.LastUpdated = timestamp;
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
                .SelectMany(t => t.Label.Split(" "))
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
                p.TeamId == ticket.TeamId);
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
                p.TeamId == ticket.TeamId);
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

        public async Task<string> ResolveApiKey(string key)
        {
            if (key.IsEmpty())
                return null;

            var entity = await Store.ResolveApiKey(key.ToSha256());

            return entity?.Id;
        }

        private async Task UpdatedSessionContext(Data.Ticket entity)
        {
            if (!entity.ChallengeId.IsEmpty())
            {
                var challenge = await Store.DbContext.Challenges.FirstOrDefaultAsync(c =>
                    c.Id == entity.ChallengeId
                );
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

        // Transform functions to create full ticket key with configurable key prefix
        private TicketSummary[] Transform(TicketSummary[] tickets) {
            return tickets.Select(x => { x.FullKey = FullKey(x.Key); return x; }).ToArray();
        }

        private Ticket Transform(Ticket ticket) {
            ticket.FullKey = FullKey(ticket.Key);
            return ticket;
        }

        private string FullKey(int key) {
            return Options.KeyPrefix+"-"+key.ToString();
        }

    }

}
