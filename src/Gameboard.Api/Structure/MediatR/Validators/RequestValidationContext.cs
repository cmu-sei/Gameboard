// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Structure.MediatR.Validators;

public class RequestValidationContext
{
    private readonly List<GameboardValidationException> _exceptions = [];
    internal IEnumerable<GameboardValidationException> ValidationExceptions { get => _exceptions; }
    internal RequestValidationContext() { }

    public RequestValidationContext AddValidationException(GameboardValidationException exception)
    {
        _exceptions.Add(exception);
        return this;
    }

    public RequestValidationContext AddValidationExceptionRange(IEnumerable<GameboardValidationException> exceptions)
    {
        _exceptions.AddRange(exceptions);
        return this;
    }
}
