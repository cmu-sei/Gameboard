using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Structure.MediatR;

internal class AuthorizationPipelineStep<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly GameboardPipelineContextService<TRequest, TResponse> _ctxService;

    public AuthorizationPipelineStep(GameboardPipelineContextService<TRequest, TResponse> ctxService)
    {
        _ctxService = ctxService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_ctxService.Context.AuthorizationRules.Evaluate())
            return await next();

        throw new ActionForbidden();
    }
}

