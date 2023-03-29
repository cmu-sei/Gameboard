using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class RequiredStringContext
{
    public string PropertyName { get; set; }
    public string Value { get; set; }
}

internal class RequiredStringValidator : IGameboardValidator<RequiredStringContext>
{

    public Task<GameboardValidationException> Validate(RequiredStringContext model)
    {
        if (string.IsNullOrWhiteSpace(model.Value))
            return Task.FromResult<GameboardValidationException>(new MissingRequiredInput<string>(model.PropertyName, model.Value));

        return Task.FromResult<GameboardValidationException>(null);
    }
}
