using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService<TModel>
{
    IValidatorService<TModel> AddValidator(IGameboardValidator<TModel> validator);
    Task Validate(TModel model);
}

internal class ValidatorService<TModel> : IValidatorService<TModel>
{
    private readonly IList<IGameboardValidator<TModel>> _validators = new List<IGameboardValidator<TModel>>();

    public IValidatorService<TModel> AddValidator(IGameboardValidator<TModel> validator)
    {
        _validators.Add(validator);
        return this;
    }

    // public IValidatorService<TModel, TProperty> AddValidator<TProperty>(IGameboardValidator<TModel> validator, )
    // {

    // }

    public async Task Validate(TModel model)
    {
        var validationExceptions = new List<GameboardValidationException>();

        foreach (var validator in _validators)
        {
            var toValidate = model;
            validationExceptions.AddIfNotNull(await validator.Validate(model));
        }

        if (validationExceptions.Count() > 0)
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(validationExceptions);
        }
    }
}
