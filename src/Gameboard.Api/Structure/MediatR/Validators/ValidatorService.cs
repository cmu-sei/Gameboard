using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService
{
    IValidatorService AddValidator(IGameboardValidator validator);
    IValidatorService AddValidator(Action<RequestValidationContext> validationAction);
    IValidatorService AddValidator(Func<RequestValidationContext, Task> validationTask);
    Task Validate();
}

internal class ValidatorService : IValidatorService
{
    private readonly IList<Func<RequestValidationContext, Task>> _validationTasks = new List<Func<RequestValidationContext, Task>>();

    public IValidatorService AddValidator(IGameboardValidator validator)
    {
        _validationTasks.Add(validator.GetValidationTask());
        return this;
    }

    public IValidatorService AddValidator(Action<RequestValidationContext> validationAction)
    {
        _validationTasks.Add(ctx => Task.Run(() => validationAction(ctx)));
        return this;
    }

    public IValidatorService AddValidator(Func<RequestValidationContext, Task> validationTask)
    {
        _validationTasks.Add(validationTask);
        return this;
    }

    public async Task Validate()
    {
        var context = new RequestValidationContext();

        foreach (var task in _validationTasks)
            await task(context);

        if (context.ValidationExceptions.Count() > 0)
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(context.ValidationExceptions);
        }
    }
}

public interface IValidatorService<TModel>
{
    IValidatorService<TModel> AddValidator(IGameboardValidator validator);
    IValidatorService<TModel> AddValidator(Action<TModel, RequestValidationContext> validationAction);
    IValidatorService<TModel> AddValidator(Func<TModel, RequestValidationContext, Task> validationTask);
    IValidatorService<TModel> AddValidator(IGameboardValidator<TModel> validator);
    Task Validate(TModel model);
}

internal class ValidatorService<TModel> : IValidatorService<TModel>
{
    private readonly IList<Func<RequestValidationContext, Task>> _nonModelValidationTasks = new List<Func<RequestValidationContext, Task>>();
    private readonly IList<Func<TModel, RequestValidationContext, Task>> _validationTasks = new List<Func<TModel, RequestValidationContext, Task>>();

    public IValidatorService<TModel> AddValidator(IGameboardValidator validator)
    {
        _nonModelValidationTasks.Add(validator.GetValidationTask());
        return this;
    }

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

    public IValidatorService<TModel> AddValidator(Action<TModel, RequestValidationContext> validationAction)
    {
        _validationTasks.Add((req, context) => Task.Run(() => validationAction(req, context)));
        return this;
    }

    public async Task Validate(TModel model)
    {
        var context = new RequestValidationContext();

        // TODO: not great that these don't happen in the order that they're added (because there are two lists). 
        // Maybe convert to delegate sig?
        foreach (var task in _validationTasks)
            await task(model, context);

        foreach (var task in _nonModelValidationTasks)
            await task(context);

        if (context.ValidationExceptions.Count() > 0)
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(context.ValidationExceptions);
        }
    }
}
