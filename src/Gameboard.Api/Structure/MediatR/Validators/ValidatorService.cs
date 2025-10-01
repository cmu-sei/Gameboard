// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService
{
    /// <summary>
    /// Ensures that an entity of the given type with the provided ID exists. Must be a type registered with EF which
    /// implements our IEntity interface.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="id"></param>
    /// <param name="isRequiredValue">
    ///     Indicates whether the value passed for id is a required property for validation. If false and if not
    ///     provided (e.g. is null or empty), validation is skipped. True by default.
    /// </param>
    /// <returns></returns>
    IValidatorService AddEntityExistsValidator<TEntity>(string id, bool isRequiredValue = true) where TEntity : class, IEntity;
    IValidatorService AddValidator(IGameboardValidator validator);
    IValidatorService AddValidator(Action<RequestValidationContext> validationAction);
    IValidatorService AddValidator(Func<RequestValidationContext, Task> validationTask);
    IValidatorService AddValidator(bool condition, GameboardValidationException ex);
    IValidatorService AddValidator(Func<Task<bool>> condition, GameboardValidationException ex);
    IValidatorService Auth(Action<IUserRolePermissionsValidator> configBuilder);
    Task Validate(CancellationToken cancellationToken);
}

internal class ValidatorService
(
    IActingUserService actingUserService,
    IStore store,
    UserRolePermissionsValidator userRolePermissionsValidator
) : IValidatorService
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IList<Func<RequestValidationContext, Task>> _validationTasks = [];
    private readonly IStore _store = store;
    private readonly UserRolePermissionsValidator _userRolePermissionsValidator = userRolePermissionsValidator;


    public IValidatorService AddEntityExistsValidator<TEntity>(string id, bool isRequiredValue = true) where TEntity : class, IEntity
    {
        if (!isRequiredValue && id.IsEmpty())
        {
            return this;
        }

        _validationTasks.Add(async ctx =>
        {
            var exists = await _store
                .WithNoTracking<TEntity>()
                .Where(e => e.Id == id)
                .AnyAsync();

            if (!exists)
            {
                ctx.AddValidationException(new ResourceNotFound<TEntity>(id));
            }
        });

        return this;
    }

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

    public IValidatorService AddValidator(bool condition, GameboardValidationException ex)
    {
        _validationTasks.Add(ctx =>
        {
            if (condition)
            {
                ctx.AddValidationException(ex);
            }

            return Task.CompletedTask;
        });

        return this;
    }

    public IValidatorService AddValidator(Func<Task<bool>> condition, GameboardValidationException ex)
    {
        _validationTasks.Add(async ctx =>
        {
            if (await condition())
            {
                ctx.AddValidationException(ex);
            }
        });

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
