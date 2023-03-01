using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Structure.MediatR;

internal class ValidationPipelineStep<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private GameboardPipelineContextService<TRequest, TResponse> _ctxService;

    public ValidationPipelineStep(GameboardPipelineContextService<TRequest, TResponse> ctxService)
    {
        _ctxService = ctxService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var ctx = _ctxService.Context;
        var validationExceptions = new List<GameboardValidationException>();

        foreach (var validator in ctx.Validators)
        {
            try
            {
                await validator.Validate(request);
            }
            catch (GameboardValidationException validationEx)
            {
                validationExceptions.Append(validationEx);
            }
        }

        if (validationExceptions.Count() == 0)
            return await next();

        throw new GameboardAggregatedValidationExceptions(validationExceptions);
    }
}
