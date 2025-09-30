// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Gameboard.Api.Structure;

abstract public class GameboardValidationException : Exception
{
    internal GameboardValidationException(string message, Exception ex = null) : base($"{message}", ex) { }
}

public class GameboardAggregatedValidationExceptions : GameboardValidationException
{
    private GameboardAggregatedValidationExceptions(string message) : base(message) { }

    internal static GameboardAggregatedValidationExceptions FromValidationExceptions(IEnumerable<GameboardValidationException> failures)
    {
        var stringBuilder = new StringBuilder("GAMEBOARD VALIDATION EXCEPTION");

        foreach (var failure in failures)
        {
            string exceptionSummary = null;
            if (failure.InnerException != null)
            {
                exceptionSummary = $" ({failure.InnerException.GetType().Name} -> {failure.InnerException.Message})";
            }

            stringBuilder.AppendLine($" - {failure.Message}{exceptionSummary ?? string.Empty}");
        }

        return new GameboardAggregatedValidationExceptions(stringBuilder.ToString());
    }

    internal static GameboardAggregatedValidationExceptions FromValidationExceptions(params GameboardValidationException[] validationExceptions)
        => FromValidationExceptions(new List<GameboardValidationException>(validationExceptions));
}
