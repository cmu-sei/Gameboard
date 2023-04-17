using System;

namespace Gameboard.Api.Structure.MediatR.Validators;

public interface IValidationPropertyProvider<TModel, TProperty>
{
    Func<TModel, TProperty> ValidationProperty { get; }
}
