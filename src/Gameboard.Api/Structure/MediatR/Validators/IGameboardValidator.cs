// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardValidator
{
    Func<RequestValidationContext, Task> GetValidationTask(CancellationToken cancellationToken);
}

public interface IGameboardValidator<TModel>
{
    Func<TModel, RequestValidationContext, Task> GetValidationTask();
}
