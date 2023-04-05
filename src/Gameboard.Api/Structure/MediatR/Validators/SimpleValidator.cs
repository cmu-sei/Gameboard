using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

// internal class SimpleValidator<TModel> : IGameboardValidator<TModel>
// {
//     public required Func<TModel, RequestValidationContext, Task> ValidateFunc { get; set; }

//     public async Task Validate(TModel model, RequestValidationContext context)
//     {
//         await ValidateFunc(model, context);
//     }
// }

// internal class SimpleValidator<TModel, TPropertyType> : IGameboardValidator<TModel>, IValidationPropertyProvider<TModel, TPropertyType>
// {
//     public required Func<TPropertyType, Task<bool>> ValidateFunc { get; set; }
//     public required Func<TModel, TPropertyType> ValidationProperty { get; set; }

//     public async Task Validate(TModel model, RequestValidationContext context)
//     {
//         var propertyValue = ValidationProperty.Invoke(model);
//         if (!(await ValidateFunc(propertyValue)))
//             context.AddValidationException(new SimpleValidatorException(ValidationFailureMessage));
//     }
// }
