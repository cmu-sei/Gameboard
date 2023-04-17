using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestValidator : IGameboardRequestValidator<GetSubmissionsQuery>
{
    private readonly EntityExistsValidator<GetSubmissionsQuery, Data.Challenge> _challengeExists;
    private readonly IPlayerStore _playerStore;
    private readonly UserRoleAuthorizer _roleAuthorizer;
    private readonly IValidatorService<GetSubmissionsQuery> _validatorService;

    public GetSubmissionsRequestValidator
    (
        EntityExistsValidator<GetSubmissionsQuery, Data.Challenge> challengeExists,
        IPlayerStore playerStore,
        UserRoleAuthorizer roleAuthorizer,
        IValidatorService<GetSubmissionsQuery> validatorService
    )
    {
        _challengeExists = challengeExists;
        _playerStore = playerStore;
        _roleAuthorizer = roleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Validate(GetSubmissionsQuery query)
    {
        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Support, UserRole.Designer };
        _roleAuthorizer.Authorize();

        _validatorService.AddValidator(_challengeExists);
        await _validatorService.Validate(query);
    }
}
