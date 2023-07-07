
namespace Gameboard.Api.Common;

public class SimpleEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public sealed class PagingParameters
{
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
}
