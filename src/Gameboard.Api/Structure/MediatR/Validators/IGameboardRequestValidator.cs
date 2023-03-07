using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

internal interface IGameboardRequestValidator<T>
{
    Task<GameboardAggregatedValidationExceptions> ValidateRequest(T request);
}