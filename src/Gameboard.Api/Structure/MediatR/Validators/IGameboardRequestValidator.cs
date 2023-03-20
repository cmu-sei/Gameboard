using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardRequestValidator<T>
{
    Task<GameboardAggregatedValidationExceptions> Validate(T input);
}
