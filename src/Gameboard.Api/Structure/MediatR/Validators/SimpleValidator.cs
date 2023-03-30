using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class SimpleValidator<TModel, TPropertyType> : IGameboardValidator<TModel>, IValidationPropertyProvider<TModel, TPropertyType>
{
    public required Func<TPropertyType, Task<bool>> IsValid { get; set; }
    public required Func<TModel, TPropertyType> ValidationProperty { get; set; }
    public required string ValidationFailureMessage { get; set; }

    public async Task<GameboardValidationException> Validate(TModel model)
    {
        var propertyValue = ValidationProperty.Invoke(model);
        if (!(await IsValid(propertyValue)))
            return new SimpleValidatorException(ValidationFailureMessage);

        return null;
    }
}
