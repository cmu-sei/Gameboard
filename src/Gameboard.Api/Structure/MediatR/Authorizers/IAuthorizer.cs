using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal interface IAuthorizer
{
    bool Authorize(User actor);
}

internal interface IAsyncAuthorizer
{
    Task<bool> Authorize(User actor);
}
