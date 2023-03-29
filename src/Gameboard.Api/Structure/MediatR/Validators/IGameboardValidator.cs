using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardValidator<TModel>
{
    Task<GameboardValidationException> Validate(TModel model);
}
