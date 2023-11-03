using Gameboard.Api.Structure.MediatR;

namespace Gameboard.Api.Validation;

public interface IValidatorServiceFactory
{
    IValidatorService Get();
    IValidatorService<TModel> Get<TModel>();
}

internal class ValidatorServiceFactory : IValidatorServiceFactory
{
    IValidatorService IValidatorServiceFactory.Get()
        => new ValidatorService();

    IValidatorService<TModel> IValidatorServiceFactory.Get<TModel>()
        => new ValidatorService<TModel>();
}
