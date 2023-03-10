using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Gameboard.Api.Structure;

abstract public class GameboardValidationException : ValidationException
{
    internal GameboardValidationException(string message, Exception ex = null) : base($"{message}", ex) { }
}

internal class GameboardAggregatedValidationExceptions : Exception
{
    internal GameboardAggregatedValidationExceptions(IEnumerable<GameboardValidationException> failures)
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
    }
}
