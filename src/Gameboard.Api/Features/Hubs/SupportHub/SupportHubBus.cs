using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Features.Support;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface ISupportHubBus
{
    Task SendTicketClosed(Ticket ticket, User closedBy);
    Task SendTicketCreated(Ticket ticket);
    Task SendTicketUpdatedBySupport(Ticket ticket, User updatedBy);
    Task SendTicketUpdatedByUser(Ticket ticket, User updatedBy);
}

internal class SupportHubBus : ISupportHubBus, IGameboardHubService
{
    private readonly IHubContext<SupportHub, ISupportHubEvent> _hubContext;
    private readonly IUserIdProvider _userIdProvider;
    private readonly ITicketAttachedUsersProvider _ticketAttachedUsersProvider;

    public SupportHubBus
    (
        IHubContext<SupportHub, ISupportHubEvent> hubContext,
        ITicketAttachedUsersProvider ticketAttachedUsersProvider,
        IUserIdProvider userIdProvider
    )
    {
        _hubContext = hubContext;
        _ticketAttachedUsersProvider = ticketAttachedUsersProvider;
        _userIdProvider = userIdProvider;
    }

    public GameboardHubType GroupType => GameboardHubType.Support;

    public async Task SendTicketClosed(Ticket ticket, User closedBy)
    {
        var evData = new TicketClosedEvent
        {
            ClosedBy = new SimpleEntity { Id = closedBy.Id, Name = closedBy.ApprovedName },
            Ticket = ToHubModel(ticket)
        };

        var attachedUsers = await _ticketAttachedUsersProvider.GetAttachedUsers(ticket.Id);
        var attachedUserIds = attachedUsers
            .Select(u => u.Id)
            // ignore the person instigating the event
            .Where(uId => uId != closedBy.Id)
            .ToArray();

        await _hubContext
            .Clients
            .Users(attachedUserIds)
            .TicketClosed(new SupportHubEvent<TicketClosedEvent>
            {
                EventType = SupportHubEventType.TicketClosed,
                Data = evData
            });
    }

    public async Task SendTicketCreated(Ticket ticket)
    {
        var evData = new TicketCreatedEvent { Ticket = ToHubModel(ticket) };
        var attachedUsers = await _ticketAttachedUsersProvider.GetAttachedUsers(ticket.Id);

        await _hubContext
            .Clients
            .Users(attachedUsers.Select(u => u.Id).Where(uId => uId != ticket.CreatorId))
            .TicketCreated(new SupportHubEvent<TicketCreatedEvent>
            {
                EventType = SupportHubEventType.TicketCreated,
                Data = evData
            });
    }

    public async Task SendTicketUpdatedBySupport(Ticket ticket, User updatedBy)
    {
        var evData = new TicketUpdatedEvent
        {
            UpdatedBy = new SimpleEntity { Id = updatedBy.Id, Name = updatedBy.ApprovedName },
            Ticket = ToHubModel(ticket)
        };

        var notifyUserIds = await _ticketAttachedUsersProvider.GetAttachedUsers(ticket.Id);

        await _hubContext
            .Clients
            .Users(notifyUserIds.Where(u => u.Id != updatedBy.Id).Select(u => u.Id))
            .TicketUpdatedBySupport(new SupportHubEvent<TicketUpdatedEvent>
            {
                EventType = SupportHubEventType.TicketUpdatedBySupport,
                Data = evData
            });
    }

    public async Task SendTicketUpdatedByUser(Ticket ticket, User updatedBy)
    {
        var evData = new TicketUpdatedEvent
        {
            UpdatedBy = new SimpleEntity { Id = updatedBy.Id, Name = updatedBy.ApprovedName },
            Ticket = ToHubModel(ticket)
        };

        var notifyUserIds = await _ticketAttachedUsersProvider.GetAttachedUsers(ticket.Id);

        await _hubContext
            .Clients
            .Users(notifyUserIds.Where(u => u.Id != updatedBy.Id).Select(u => u.Id))
            .TicketUpdatedByUser(new SupportHubEvent<TicketUpdatedEvent>
            {
                EventType = SupportHubEventType.TicketUpdatedByUser,
                Data = evData
            });
    }

    private SupportHubTicket ToHubModel(Ticket ticket)
    {
        return new SupportHubTicket
        {
            Id = ticket.Key.ToString(),
            Key = ticket.FullKey,
            Summary = ticket.Summary,
            Description = ticket.Description,
            Status = ticket.Status,
            CreatedBy = new SimpleEntity
            {
                Id = ticket.CreatorId,
                Name = ticket.Creator?.Name
            }
        };
    }
}
