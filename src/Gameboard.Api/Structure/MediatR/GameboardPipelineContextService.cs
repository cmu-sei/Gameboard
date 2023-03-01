using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Structure;

internal class GameboardPipelineContextService<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    public GameboardRequestContext<TRequest, TResponse> Context { get; set; }
}
