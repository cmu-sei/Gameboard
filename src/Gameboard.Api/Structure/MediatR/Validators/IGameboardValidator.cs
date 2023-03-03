using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardValidator
{
    Task<GameboardValidationException> Validate<TModel>(TModel model);
}

public interface IGameboardValidator<TModel, TException> where TModel : class where TException : GameboardValidationException
{
    Task<TException> Validate(TModel model);
}
