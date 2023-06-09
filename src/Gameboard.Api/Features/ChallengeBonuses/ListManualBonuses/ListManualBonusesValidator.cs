using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ListManualBonusesValidator : IGameboardRequestValidator<ListManualBonusesQuery>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly EntityExistsValidator<ListManualBonusesQuery, Data.Challenge> _challengeExists;
    private readonly EntityExistsValidator<ListManualBonusesQuery, Data.User> _userExists;
    private readonly IValidatorService<ListManualBonusesQuery> _validatorService;

    public ListManualBonusesValidator
    (
        UserRoleAuthorizer authorizer,
        EntityExistsValidator<ListManualBonusesQuery, Data.Challenge> challengeExists,
        EntityExistsValidator<ListManualBonusesQuery, Data.User> userExists,
        IValidatorService<ListManualBonusesQuery> validatorService
    )
    {
        _authorizer = authorizer;
        _challengeExists = challengeExists;
        _userExists = userExists;
        _validatorService = validatorService;
    }

    public async Task Validate(ListManualBonusesQuery request)
    {
        _authorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Support)
            .Authorize();

        _validatorService
            .AddValidator(_challengeExists)
            .AddValidator(_userExists);
        await _validatorService.Validate(request);
    }
}
