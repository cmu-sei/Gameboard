using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ListManualBonusesValidator : IGameboardRequestValidator<ListManualBonusesQuery>
{
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists;
    private readonly UserRoleAuthorizer _authorizer;

    public ListManualBonusesValidator(
        UserRoleAuthorizer authorizer,
        EntityExistsValidator<Data.Challenge> challengeExists,
        EntityExistsValidator<Data.User> userExists,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizer = authorizer;
        _challengeExists = challengeExists;
    }

    public async Task<GameboardAggregatedValidationExceptions> Validate(ListManualBonusesQuery input)
    {
        _authorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Designer, UserRole.Support };
        _authorizer.Authorize();

        var exceptions = new List<GameboardValidationException>()
            .AddIfNotNull(await _challengeExists.Validate(input.challengeId));

        if (exceptions.Count() > 0)
            return GameboardAggregatedValidationExceptions.FromValidationExceptions(exceptions);

        return null;
    }
}
