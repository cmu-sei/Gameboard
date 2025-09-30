// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Structure.MediatR.Validators;

public interface IValidationPropertyProvider<TModel, TProperty>
{
    Func<TModel, TProperty> ValidationProperty { get; }
}
