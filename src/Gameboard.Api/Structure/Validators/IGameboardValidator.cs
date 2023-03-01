using System.Threading.Tasks;

namespace Gameboard.Api.Structure;

public interface IGameboardValidator<T> where T : class
{
    Task<GameboardValidationException> Validate(T model);
}
