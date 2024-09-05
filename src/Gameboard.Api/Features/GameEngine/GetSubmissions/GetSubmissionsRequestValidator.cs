using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestValidator(
    EntityExistsValidator<GetSubmissionsQuery, Data.Challenge> challengeExists,
    IValidatorService<GetSubmissionsQuery> validatorService
    ) : IGameboardRequestValidator<GetSubmissionsQuery>
{
    private readonly EntityExistsValidator<GetSubmissionsQuery, Data.Challenge> _challengeExists = challengeExists;
    private readonly IValidatorService<GetSubmissionsQuery> _validatorService = validatorService;

    public async Task Validate(GetSubmissionsQuery query, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.RequirePermissions(Users.PermissionKey.Teams_Observe))
            .AddValidator(_challengeExists)
            .Validate(query, cancellationToken);
    }
}
