using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Authorizers;

internal interface IAuthorizer
{
    void Authorize();
}

internal interface IAsyncAuthorizer
{
    Task Authorize();
}
