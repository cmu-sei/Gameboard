using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface ISupportHubBus
{
    Task SendTicketClosed(Ticket ticket, User closedBy);
    Task SendTicketCreated(Ticket ticket);
}

internal class SupportHubBus : ISupportHubBus, IGameboardHubBus
{
    private readonly IHubContext<SupportHub, ISupportHubEvent> _hubContext;

    public SupportHubBus(IHubContext<SupportHub, ISupportHubEvent> hubContext)
    {
        _hubContext = hubContext;
    }

    public GameboardHubGroupType GroupType => GameboardHubGroupType.Score;

    public async Task SendTicketClosed(Ticket ticket, User closedBy)
    {
        var evData = new TicketClosedEvent
        {
            ClosedBy = new SimpleEntity { Id = closedBy.Id, Name = closedBy.ApprovedName },
            Ticket = ToHubModel(ticket)
        };

        await _hubContext
            .Clients
            .AllExcept(closedBy.Id)
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
            .AllExcept(ticket.CreatorId)
            .TicketCreated(new SupportHubEvent<TicketCreatedEvent>
            {
                EventType = SupportHubEventType.TicketCreated,
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
