namespace Gameboard.Api.Features.ChallengeBonuses;

using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.AspNetCore.Http;

internal class AddManualBonusValidator : IGameboardRequestValidator<AddManualBonusCommand>
{
    private readonly EntityExistsValidator<AddManualBonusCommand, Data.Challenge> _challengeExists;
    private readonly User _actor;
    private readonly IValidatorService<AddManualBonusCommand> _validatorService;

    public AddManualBonusValidator
    (
        EntityExistsValidator<AddManualBonusCommand, Data.Challenge> challengeExists,
        IHttpContextAccessor httpContextAccessor,
        IValidatorService<AddManualBonusCommand> validatorService
    )
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeExists = challengeExists;
        _validatorService = validatorService;
    }

    public async Task Validate(AddManualBonusCommand request)
    {
        _validatorService.AddValidator((request, context) =>
        {
            if (request.Model.PointValue <= 0)
                context.AddValidationException(new InvalidParameterValue<double>(nameof(request.Model.PointValue), "Must be greater than zero.", request.Model.PointValue));

            return Task.CompletedTask;
        });

        _validatorService.AddValidator((request, context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Model.Description))
                context.AddValidationException(new MissingRequiredInput<string>(nameof(request.Model.Description), request.Model.Description));

            return Task.CompletedTask;
        });

        _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));

        await _validatorService.Validate(request);
    }
}
