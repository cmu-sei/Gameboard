using System.Collections.Generic;

namespace Gameboard.Api.Structure.MediatR.Validators;

public class RequestValidationContext
{
    private List<GameboardValidationException> _exceptions = new List<GameboardValidationException>();
    internal IEnumerable<GameboardValidationException> ValidationExceptions { get => _exceptions; }
    internal RequestValidationContext() { }

    public void AddValidationException(GameboardValidationException exception)
    {
        _exceptions.Add(exception);
    }
}
