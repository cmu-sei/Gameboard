using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class SimpleValidator<TModel> : IGameboardRequestValidator<TModel>
{
    private readonly Func<TModel, bool> _isValid;
    private readonly string _validationFailureMessage;

    public SimpleValidator(Func<TModel, bool> isValid, string validationFailureMessage)
    {
        _isValid = isValid;
        _validationFailureMessage = validationFailureMessage;
    }

    public Task<GameboardAggregatedValidationExceptions> Validate(TModel request)
        => Task.FromResult(_isValid(request) ? null : GameboardAggregatedValidationExceptions.FromValidationExceptions(new SimpleValidatorException(_validationFailureMessage)));

    private class SimpleValidatorException : GameboardValidationException
    {
        public SimpleValidatorException(string message, Exception ex = null) : base(message, ex)
        {
        }
    }
}
