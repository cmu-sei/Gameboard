namespace Gameboard.Api.Features.ChallengeBonuses;

using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.AspNetCore.Http;

internal class AddManualBonusValidator : IGameboardRequestValidator<AddManualBonusCommand>
{
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists;
    private readonly RequiredStringValidator _descriptionRequired;
    private readonly User _actor;

    public AddManualBonusValidator
    (
        EntityExistsValidator<Data.Challenge> challengeExists,
        RequiredStringValidator descriptionRequired,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeExists = challengeExists;
        _descriptionRequired = descriptionRequired;
    }

    public async Task<GameboardAggregatedValidationExceptions> Validate(AddManualBonusCommand request)
    {
        var pointsValidator = new SimpleValidator<double>(d => d > 0.0, $"{nameof(request.model.PointValue)} must be positive.");

        var validationExceptions = new List<GameboardValidationException>()
            .AddIfNotNull(await _challengeExists.Validate(request.challengeId))
            .AddIfNotNull(await pointsValidator.Validate(request.model.PointValue))
            .AddIfNotNull(await _descriptionRequired.Validate(new RequiredStringContext
            {
                PropertyName = nameof(request.model.Description),
                Value = request.model.Description
            }));

        return GameboardAggregatedValidationExceptions.FromValidationExceptions(validationExceptions);
    }
}
