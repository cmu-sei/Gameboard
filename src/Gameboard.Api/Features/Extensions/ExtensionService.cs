using System.Threading.Tasks;

namespace Gameboard.Api.Features.Extensions;

[DontBindForDI]
public interface IExtensionService
{
    Task NotifyScored(ExtensionMessage message);
    Task NotifyTicketCreated(ExtensionMessage message);
}
