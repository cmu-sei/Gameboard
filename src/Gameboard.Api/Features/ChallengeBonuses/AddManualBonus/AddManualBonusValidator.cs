namespace Gameboard.Api.Features.ChallengeBonuses;

using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.AspNetCore.Http;

internal class AddManualBonusValidator : IGameboardRequestValidator<AddManualBonusCommand>
{
    private readonly EntityExistsValidator<AddManualBonusCommand, Data.Challenge> _challengeExists;
    private readonly RequiredStringValidator _descriptionRequired;
    private readonly User _actor;
    private readonly IValidatorService<AddManualBonusCommand> _validatorService;

    public AddManualBonusValidator
    (
        EntityExistsValidator<AddManualBonusCommand, Data.Challenge> challengeExists,
        RequiredStringValidator descriptionRequired,
        IHttpContextAccessor httpContextAccessor,
        IValidatorService<AddManualBonusCommand> validatorService
    )
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeExists = challengeExists;
        _descriptionRequired = descriptionRequired;
        _validatorService = validatorService;
    }

    public async Task Validate(AddManualBonusCommand request)
    {
        var pointsValidator = new SimpleValidator<AddManualBonusCommand, double>
        {
            ValidationProperty = c => c.Model.PointValue,
            IsValid = d => Task.FromResult(d > 0.0),
            ValidationFailureMessage = $"{nameof(request.Model.PointValue)} must be positive."
        };

        var descriptionRequired = new SimpleValidator<AddManualBonusCommand, string>
        {
            ValidationProperty = c => c.Model.Description,
            IsValid = d => Task.FromResult(!string.IsNullOrWhiteSpace(d)),
            ValidationFailureMessage = $"{nameof(request.Model.Description)} is required."
        };

        _validatorService
            .AddValidator(pointsValidator)
            .AddValidator(_challengeExists.UseProperty(r => r.ChallengeId))
            .AddValidator(descriptionRequired);

        await _validatorService.Validate(request);
    }
}
