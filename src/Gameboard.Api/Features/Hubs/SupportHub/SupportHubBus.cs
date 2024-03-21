using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface ISupportHubBus
{
    Task SendTicketClosed(Ticket ticket, User closedBy);
    Task SendTicketCreated(Ticket ticket);
    Task SendTicketUpdatedBySupport(Ticket ticket, User updatedBy);
    Task SendTicketUpdatedByUser(Ticket ticket, User updatedBy);
}

internal class SupportHubBus : ISupportHubBus, IGameboardHubBus
{
    private readonly IHubContext<SupportHub, ISupportHubEvent> _hubContext;

    public SupportHubBus(IHubContext<SupportHub, ISupportHubEvent> hubContext)
    {
        _hubContext = hubContext;
    }

    public GameboardHubType GroupType => GameboardHubType.Support;

    public async Task SendTicketClosed(Ticket ticket, User closedBy)
    {
        var evData = new TicketClosedEvent
        {
            ClosedBy = new SimpleEntity { Id = closedBy.Id, Name = closedBy.ApprovedName },
            Ticket = ToHubModel(ticket)
        };

        await _hubContext
            .Clients
            .GroupExcept(this.GetCanonicalGroupId(SupportHub.GROUP_STAFF), _userHubConnections.GetConnections(closedBy.Id))
            .TicketClosed(new SupportHubEvent<TicketClosedEvent>
            {
                EventType = SupportHubEventType.TicketClosed,
                Data = evData
            });
    }

    public async Task SendTicketCreated(Ticket ticket)
    {
        var evData = new TicketCreatedEvent { Ticket = ToHubModel(ticket) };

        await _hubContext
            .Clients
            .GroupExcept(this.GetCanonicalGroupId(SupportHub.GROUP_STAFF), _userHubConnections.GetConnections(ticket.CreatorId))
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

        await _hubContext
            .Clients
            .GroupExcept(this.GetCanonicalGroupId(SupportHub.GROUP_STAFF), _userHubConnections.GetConnections(updatedBy.Id))
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

        await _hubContext
            .Clients
            .GroupExcept(this.GetCanonicalGroupId(SupportHub.GROUP_STAFF), _userHubConnections.GetConnections(updatedBy.Id))
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
                Name = ticket.Creator.ApprovedName
            }
        };
    }
}
