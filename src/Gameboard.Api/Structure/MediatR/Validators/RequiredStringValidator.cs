using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class RequiredStringValidator : IGameboardValidator<string, MissingRequiredInput<string>>
{
    public string NameOfStringProperty { get; set; }

    public Task<MissingRequiredInput<string>> Validate(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return Task.FromResult(new MissingRequiredInput<string>(NameOfStringProperty, model));

        return null;
    }
}
