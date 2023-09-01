using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService<TModel>
{
    IValidatorService<TModel> AddValidator(Func<TModel, RequestValidationContext, Task> validationTask);
    IValidatorService<TModel> AddValidator(IGameboardValidator<TModel> validator);
    Task Validate(TModel model);
}

internal class ValidatorService<TModel> : IValidatorService<TModel>
{
    private readonly IList<Func<TModel, RequestValidationContext, Task>> _validationTasks = new List<Func<TModel, RequestValidationContext, Task>>();

    public IValidatorService<TModel> AddValidator(IGameboardValidator<TModel> validator)
    {
        _validationTasks.Add(validator.GetValidationTask());
        return this;
    }

    public IValidatorService<TModel> AddValidator(Func<TModel, RequestValidationContext, Task> validationTask)
    {
        _validationTasks.Add(validationTask);
        return this;
    }

    public async Task Validate(TModel model)
    {
        var context = new RequestValidationContext();

        foreach (var task in _validationTasks)
        {
            await task(model, context);
        }

        if (context.ValidationExceptions.Any())
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(context.ValidationExceptions);
        }
    }
}
