using System;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardValidator
{
    Func<RequestValidationContext, Task> GetValidationTask();
}

public interface IGameboardValidator<TModel>
{
    Func<TModel, RequestValidationContext, Task> GetValidationTask();
}
