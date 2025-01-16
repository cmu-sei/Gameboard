// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Support;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Hubs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public class TicketService
(
    IActingUserService actingUserService,
    IAutoTagService autoTagService,
    IFileUploadService fileUploadService,
    IGuidService guids,
    ILogger<TicketService> logger,
    IMapper mapper,
    IMediator mediator,
    INowService now,
    CoreOptions options,
    IUserRolePermissionsService permissionsService,
    IStore store,
    ISupportHubBus supportHubBus,
    ITeamService teamService
) : _Service(logger, mapper, options)
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IAutoTagService _autoTagService = autoTagService;
    private readonly IFileUploadService _fileUploadService = fileUploadService;
    private readonly IGuidService _guids = guids;
    private readonly IMediator _mediator = mediator;
    private readonly INowService _now = now;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;
    private readonly ISupportHubBus _supportHubBus = supportHubBus;
    private readonly ITeamService _teamService = teamService;

    internal static char TAGS_DELIMITER = ' ';

    public string GetFullKey(int key)
        => $"{(Options.KeyPrefix.IsEmpty() ? "GB" : Options.KeyPrefix)}-{key}";

    public Task<Ticket> Retrieve(string id, SortDirection activitySortDirection = SortDirection.Asc)
        => LoadTicketDto(id, activitySortDirection);

    public Task<Ticket> Retrieve(int id, SortDirection activitySortDirection = SortDirection.Asc)
        => LoadTicketDto(id, activitySortDirection);

    public IQueryable<Data.Ticket> BuildTicketSearchQuery(string term)
    {
        var q = BuildTicketQueryBase();

        if (term.NotEmpty())
        {
            term = term.ToLower();
            var prefix = Options.KeyPrefix.ToLower() + "-";
            q = q.Where
            (
                t =>
                    t.Summary.ToLower().Contains(term) ||
                    t.Label.ToLower().Contains(term) ||
                    (prefix + t.Key.ToString()).Contains(term) ||
                    t.Requester.ApprovedName.ToLower().Contains(term) ||
                    t.Assignee.ApprovedName.ToLower().Contains(term) ||
                    t.Challenge.Name.ToLower().Contains(term) ||
                    t.Challenge.Tag.ToLower().Contains(term) ||
                    t.Challenge.Id.ToLower() == term ||
                    t.TeamId.ToLower().Contains(term) ||
                    t.PlayerId.ToLower().Contains(term) ||
                    t.RequesterId.ToLower().Contains(term)
            );
        }

        return q;
    }

    public async Task<Ticket> Create(NewTicket model)
    {
        Data.Ticket entity;
        var timestamp = _now.Get();
        var actingUser = _actingUserService.Get();
        var canManageTickets = await _permissionsService.Can(PermissionKey.Support_ManageTickets);

        if (canManageTickets)
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
        entity.Id = _guids.Generate();

        // upload files
        var uploads = await _fileUploadService.Upload(Path.Combine(Options.SupportUploadsFolder, entity.Id), model.Uploads);
        if (uploads.Any())
        {
            var fileNames = uploads.Select(x => x.FileName).ToArray();
            entity.Attachments = Mapper.Map<string>(fileNames);
        }

        await _store.Create(entity);
        var createdTicketModel = await LoadTicketDto(entity.Id);

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

    public IQueryable<Data.Ticket> GetGameTicketsQuery(string gameId)
        => _store
            .WithNoTracking<Data.Ticket>()
            .Where(t => t.Challenge.GameId == gameId || t.Player.Challenges.Any(c => c.GameId == gameId));

    public IQueryable<Data.Ticket> GetGameOpenTicketsQuery(string gameId)
        => GetGameTicketsQuery(gameId).Where(t => t.Status != "Closed");

    public IQueryable<Data.Ticket> GetTeamTickets(IEnumerable<string> teamIds)
        => _store
            .WithNoTracking<Data.Ticket>()
            .Where(t => teamIds.Contains(t.TeamId));

    public async Task<Ticket> Update(ChangedTicket model, string actorId, bool sudo)
    {
        // need the creator to send updates
        var entity = await _store
            .WithTracking<Data.Ticket>()
            .Include(t => t.Creator)
            .SingleAsync(t => t.Id == model.Id);
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
            {
                updateClosesTicket = true;
            }

            updatedBySupport = true;
        }
        else // regular participant can only edit a few fields
        {
            Mapper.Map(Mapper.Map<SelfChangedTicket>(model), entity);
            updatedByUser = true;
        }

        entity.LastUpdated = timestamp;

        await _store.SaveUpdate(entity, default);
        var updatedTicketModel = await LoadTicketDto(entity.Id);

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
        var q = BuildTicketSearchQuery(model.Term);

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
            var userTeams = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.UserId == userId && p.TeamId != null && p.TeamId != "")
                .Select(p => p.TeamId)
                .Distinct()
                .ToListAsync();

            q = q.Where(t => t.RequesterId == userId ||
                userTeams.Any(i => i == t.TeamId));
        }

        if (model.GameId.IsNotEmpty())
            q = q.Where(t => t.Challenge.GameId == model.GameId || t.Player.GameId == model.GameId);

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
        var entity = await _store
            .WithTracking<Data.Ticket>()
            .SingleAsync(t => t.Id == model.TicketId);
        var timestamp = _now.Get();
        var actingUser = _actingUserService.Get();

        var commentActivity = new Data.TicketActivity
        {
            Id = _guids.Generate(),
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
        await _store.SaveUpdate(entity, default);

        var result = Mapper.Map<TicketActivity>(commentActivity);
        result.RequesterId = entity.RequesterId;
        result.LastUpdated = entity.LastUpdated;
        result.Key = entity.Key;
        result.Status = entity.Status;

        // send signalR/browser notifications for updates
        var ticketDto = await LoadTicketDto(model.TicketId);

        if (await _permissionsService.Can(PermissionKey.Support_ManageTickets))
            await _supportHubBus.SendTicketUpdatedBySupport(ticketDto, actingUser);
        else
            await _supportHubBus.SendTicketUpdatedByUser(ticketDto, actingUser);

        return result;
    }

    public async Task<string[]> ListLabels(SearchFilter model)
    {
        var q = BuildTicketSearchQuery(model.Term);
        var tickets = await Mapper.ProjectTo<TicketSummary>(q).ToArrayAsync();

        var b = tickets
            .Where(t => !t.Label.IsEmpty())
            .SelectMany(t => TransformTicketLabels(t.Label))
            .OrderBy(t => t)
            .Distinct()
            .ToArray();

        return b;
    }

    public async Task<bool> IsOwnerOrTeamMember(int ticketId, string userId)
    {
        var ticket = await _store
            .WithNoTracking<Data.Ticket>()
            .Select(t => new
            {
                t.Id,
                t.Key,
                t.RequesterId,
                t.TeamId
            })
            .SingleOrDefaultAsync(t => t.Key == ticketId);

        if (ticket == null)
            return false;
        if (ticket.RequesterId == userId)
            return true;
        if (ticket.TeamId.IsEmpty())
            return false;

        // if team associated with ticket, see if this user has an enrollment with matching teamId
        return await _store.AnyAsync<Data.Player>(p =>
            p.UserId == userId &&
            p.TeamId == ticket.TeamId
        , CancellationToken.None);
    }

    public async Task<bool> IsOwnerOrTeamMember(string ticketId, string userId)
    {
        var ticket = await _store
            .WithNoTracking<Data.Ticket>()
            .Select(t => new
            {
                t.Id,
                t.RequesterId,
                t.TeamId
            })
            .SingleOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null)
            return false;
        if (ticket.RequesterId == userId)
            return true;
        if (ticket.TeamId.IsEmpty())
            return false;

        // if team associated with ticket, see if this user has an enrollment with matching teamId
        return await _store.AnyAsync<Data.Player>(p =>
            p.UserId == userId &&
            p.TeamId == ticket.TeamId
        , CancellationToken.None);
    }

    public async Task<bool> UserCanUpdate(string ticketId, string userId)
    {
        var ticket = await BuildTicketSearchQuery(ticketId).SingleOrDefaultAsync();
        if (ticket == null)
            return false;

        var updateUntilTime = DateTimeOffset.UtcNow.Add(new TimeSpan(0, -5, 0));

        return ticket.RequesterId == userId && ticket.Created > updateUntilTime;
    }

    internal IEnumerable<string> TransformTicketLabels(string labels)
    {
        if (labels.IsEmpty())
            return [];

        return labels.Split(TAGS_DELIMITER, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task UpdatedSessionContext(Data.Ticket entity)
    {
        if (!entity.ChallengeId.IsEmpty())
        {
            var challenge = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == entity.ChallengeId)
                .SingleOrDefaultAsync();

            if (challenge is not null)
            {
                entity.TeamId = challenge.TeamId;
                entity.PlayerId = challenge.PlayerId;
            }
        }
        else if (!entity.PlayerId.IsEmpty())
        {
            var player = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.Id == entity.PlayerId)
                .SingleOrDefaultAsync();

            if (player != null)
            {
                entity.TeamId = player.TeamId;
                entity.ChallengeId = null;
            }
        }

        // add conditional auto-tags
        var autoTags = await _autoTagService.GetAutoTags(entity, CancellationToken.None);
        var finalTags = new List<string>(entity.Label?.Split(TAGS_DELIMITER, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);

        foreach (var autoTag in autoTags)
        {
            if (!finalTags.Contains(autoTag))
            {
                finalTags.Add(autoTag.Trim());
            }
        }

        entity.Label = string.Join(TAGS_DELIMITER, finalTags);

        if (entity.TeamId.IsEmpty())
        {
            entity.TeamId = null;
            entity.ChallengeId = null;
            entity.PlayerId = null;
        }
    }

    private void AddActivity(Data.Ticket entity, string actorId, bool statusChanged, bool assigneeChanged, DateTimeOffset timestamp)
    {
        if (statusChanged)
        {
            var statusActivity = new Data.TicketActivity
            {
                Id = _guids.Generate(),
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
                Id = _guids.Generate(),
                UserId = actorId,
                AssigneeId = entity.AssigneeId,
                Type = ActivityType.AssigneeChange,
                Timestamp = timestamp
            };
            entity.Activity.Add(assigneeActivity);
        }
    }

    private async Task<Ticket> LoadTicketDto(int ticketKey, SortDirection activitySortDirection = SortDirection.Asc)
        => await BuildTicketDto(await BuildTicketQueryBase().SingleOrDefaultAsync(t => t.Key == ticketKey), activitySortDirection);

    private async Task<Ticket> LoadTicketDto(string ticketId, SortDirection activitySortDirection = SortDirection.Asc)
        => await BuildTicketDto(await BuildTicketQueryBase().SingleOrDefaultAsync(t => t.Id == ticketId), activitySortDirection);

    private async Task<Ticket> BuildTicketDto(Data.Ticket ticketEntity, SortDirection activitySortDirection)
    {
        var ticket = Mapper.Map<Ticket>(ticketEntity);
        ticket.FullKey = GetFullKey(ticket.Key);

        ticket.Assignee = await BuildTicketUser(ticketEntity.Assignee);
        ticket.Creator = await BuildTicketUser(ticketEntity.Creator);
        ticket.Requester = await BuildTicketUser(ticketEntity.Requester);
        ticket.TeamName = "(deleted team)";

        if (ticket.TeamId.IsNotEmpty())
        {
            // #just553things
            var captain = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == ticket.TeamId)
                .OrderBy(p => p.Role == PlayerRole.Manager ? 0 : 1)
                .FirstOrDefaultAsync();

            if (captain is not null)
            {
                ticket.TeamName = captain.ApprovedName;
            }
        }

        if (ticket.Player is not null)
        {
            ticket.TimeTilSessionEndMs = ticket.Player.SessionEnd.IsEmpty() ? default(double?) : (ticket.Player.SessionEnd - _now.Get()).TotalMilliseconds;
        }

        if (ticket.Challenge is not null)
        {
            ticket.IsTeamGame = ticket.Challenge.AllowTeam;
        }

        if (activitySortDirection == SortDirection.Asc)
        {
            ticket.Activity = [.. ticket.Activity.OrderBy(a => a.Timestamp)];
        }
        else
        {
            ticket.Activity = [.. ticket.Activity.OrderByDescending(a => a.Timestamp)];
        }

        // hydrate "is support personnel" across all activity
        var activityUsers = ticket.Activity.Select(a => a.User.Id).ToArray();
        var activityUserRoles = await _store
            .WithNoTracking<Data.User>()
            .Select(u => new { u.Id, u.Role })
            .ToDictionaryAsync(u => u.Id, u => u.Role, CancellationToken.None);

        foreach (var activity in ticket.Activity)
        {
            if (activityUserRoles.TryGetValue(activity.User.Id, out var activityUserRole))
            {
                activity.User.IsSupportPersonnel = await _permissionsService.Can(activityUserRole, PermissionKey.Support_ManageTickets);
            }
        }

        return ticket;
    }

    private async Task<TicketUser> BuildTicketUser(Data.User user)
    {
        if (user is null)
            return null;

        return new TicketUser()
        {
            Name = user.ApprovedName,
            Id = user.Id,
            IsSupportPersonnel = await _permissionsService.Can(user.Role, PermissionKey.Support_ManageTickets)
        };
    }

    private IQueryable<Data.Ticket> BuildTicketQueryBase()
        => _store
            .WithNoTracking<Data.Ticket>()
            .Include(c => c.Requester)
            .Include(c => c.Assignee)
            .Include(c => c.Creator)
            .Include(c => c.Activity)
                .ThenInclude(a => a.User)
            .Include(c => c.Activity)
                .ThenInclude(a => a.Assignee)
            .Include(c => c.Challenge)
                .ThenInclude(c => c.Game)
            .Include(c => c.Player)
                .ThenInclude(p => p.Game);
}
