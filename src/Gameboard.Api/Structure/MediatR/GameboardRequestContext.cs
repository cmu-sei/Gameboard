using System;
using System.Collections.Generic;
using MediatR;

namespace Gameboard.Api.Structure.MediatR;

public class GameboardRequestContext<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    public readonly User Actor;
    public readonly TRequest Request;
    public readonly GameboardAuthorizationRules AuthorizationRules;
    public readonly IList<IGameboardValidator<TRequest>> Validators;

    public GameboardRequestContext(TRequest request, User actor)
    {
        Request = request;
        Actor = actor;
        AuthorizationRules = new GameboardAuthorizationRules(actor, new List<Func<bool>>());
        Validators = new List<IGameboardValidator<TRequest>>();
    }
}
