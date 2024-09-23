using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService
{
    IValidatorService AddValidator(IGameboardValidator validator);
    IValidatorService AddValidator(Action<RequestValidationContext> validationAction);
    IValidatorService AddValidator(Func<RequestValidationContext, Task> validationTask);
    IValidatorService Auth(Action<IUserRolePermissionsValidator> configBuilder);
    Task Validate(CancellationToken cancellationToken);
}

internal class ValidatorService(IActingUserService actingUserService, UserRolePermissionsValidator userRolePermissionsValidator) : IValidatorService
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IList<Func<RequestValidationContext, Task>> _validationTasks = [];
    private readonly UserRolePermissionsValidator _userRolePermissionsValidator = userRolePermissionsValidator;

    public IValidatorService AddValidator(IGameboardValidator validator)
    {
        _validationTasks.Add(validator.GetValidationTask(default));
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

    public IValidatorService Auth(Action<IUserRolePermissionsValidator> configBuilder)
    {
        configBuilder(_userRolePermissionsValidator);
        return this;
    }

    public async Task Validate(CancellationToken cancellationToken)
    {
        var context = new RequestValidationContext();
        var actingUser = _actingUserService.Get();
        var authValidationExceptions = await _userRolePermissionsValidator.GetAuthValidationExceptions(actingUser);

        if (authValidationExceptions.Any())
        {
            context.AddValidationExceptionRange(authValidationExceptions);
        }
        else
        {
            foreach (var task in _validationTasks)
                await task(context);
        }

        if (context.ValidationExceptions.Any())
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
    IValidatorService<TModel> Auth(Action<IUserRolePermissionsValidator> configBuilder);
    Task Validate(TModel model, CancellationToken cancellationToken);
}

internal class ValidatorService<TModel>(IActingUserService actingUserService, UserRolePermissionsValidator userRolePermissionsValidator) : IValidatorService<TModel>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly UserRolePermissionsValidator _userRolePermissionsValidator = userRolePermissionsValidator;
    private readonly IList<Func<RequestValidationContext, Task>> _nonModelValidationTasks = [];
    private readonly IList<Func<TModel, RequestValidationContext, Task>> _validationTasks = [];

    public IValidatorService<TModel> AddValidator(IGameboardValidator validator)
    {
        _nonModelValidationTasks.Add(validator.GetValidationTask(default));
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

    public IValidatorService<TModel> Auth(Action<IUserRolePermissionsValidator> configBuilder)
    {
        ArgumentNullException.ThrowIfNull(configBuilder);
        configBuilder(_userRolePermissionsValidator);
        return this;
    }

    public async Task Validate(TModel model, CancellationToken cancellationToken)
    {
        var context = new RequestValidationContext();
        var actingUser = _actingUserService.Get();
        var authValidationExceptions = await _userRolePermissionsValidator.GetAuthValidationExceptions(actingUser);

        if (authValidationExceptions.Any())
        {
            context.AddValidationExceptionRange(authValidationExceptions);
        }
        else
        {
            // TODO: not great that these don't happen in the order that they're added (because there are two lists). 
            // Maybe convert to delegate sig?
            foreach (var task in _validationTasks)
                await task(model, context);

            foreach (var task in _nonModelValidationTasks)
                await task(context);
        }

        if (context.ValidationExceptions.Any())
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(context.ValidationExceptions);
        }
    }
}
