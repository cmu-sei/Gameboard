using System;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Structure;

public interface IGameboardMediator<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    Task<TResponse> Send(IRequest<TResponse> request, Action<GameboardRequestContext<TRequest, TResponse>> contextBuilder);
}

internal class GameboardMediator<TRequest, TResponse> : IGameboardMediator<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly GameboardPipelineContextService<TRequest, TResponse> _ctxService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;

    public GameboardMediator(IHttpContextAccessor httpContext, GameboardPipelineContextService<TRequest, TResponse> contextService, IMediator mediator)
    {
        _ctxService = contextService;
        _httpContextAccessor = httpContext;
        _mediator = mediator;
    }

    public async Task<TResponse> Send(IRequest<TResponse> request, Action<GameboardRequestContext<TRequest, TResponse>> contextBuilder)
    {
        var user = _httpContextAccessor.HttpContext.User.ToActor();
        var context = new GameboardRequestContext<TRequest, TResponse>((TRequest)request, _httpContextAccessor.HttpContext.User.ToActor());
        contextBuilder.Invoke(context);
        _ctxService.Context = context;

        return await _mediator.Send(request);
    }
}
