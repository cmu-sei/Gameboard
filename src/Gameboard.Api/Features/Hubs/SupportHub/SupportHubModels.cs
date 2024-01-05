using System.Threading.Tasks;

namespace Gameboard.Api.Hubs;

public class SupportHubEvent<TData> where TData : class
{
    public required SupportHubEventType EventType { get; set; }
    public required TData Data { get; set; }
}

public enum SupportHubEventType
{
    TicketClosed,
    TicketCreated,
    TicketUpdatedByUser,
    TicketUpdatedBySupport
}

public sealed class TicketClosedEvent
{
    public required SimpleEntity ClosedBy { get; set; }
    public required SupportHubTicket Ticket { get; set; }
}

public sealed class TicketCreatedEvent
{
    public required SupportHubTicket Ticket { get; set; }
}

public sealed class SupportHubTicket
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public required string Summary { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
    public required SimpleEntity CreatedBy { get; set; }
}

public interface ISupportHubEvent
{
    Task TicketClosed(SupportHubEvent<TicketClosedEvent> ev);
    Task TicketCreated(SupportHubEvent<TicketCreatedEvent> ev);
}
