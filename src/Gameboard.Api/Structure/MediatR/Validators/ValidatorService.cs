using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IValidatorService
{
    Task Validate<T>(T request, params IGameboardValidator[] validators);
    Task Validate<T>(T request, IEnumerable<IGameboardValidator> validators);
}

internal class ValidatorService : IValidatorService
{
    public async Task Validate<T>(T request, IEnumerable<IGameboardValidator> validators)
    {
        var validationExceptions = new List<GameboardValidationException>();

        foreach (var validator in validators)
            validationExceptions.AddIfNotNull(await validator.Validate(request));

        if (validationExceptions.Count() > 0)
        {
            throw GameboardAggregatedValidationExceptions.FromValidationExceptions(validationExceptions);
        }
    }

    public Task Validate<T>(T request, params IGameboardValidator[] validators)
        => Validate(request, new List<IGameboardValidator>(validators));
}
