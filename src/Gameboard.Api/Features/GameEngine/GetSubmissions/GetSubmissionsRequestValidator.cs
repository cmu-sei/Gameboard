using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestValidator : IGameboardRequestValidator<GetSubmissionsQuery>
{
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists;
    private readonly TeamExistsValidator _teamExists;
    private readonly User _actor;

    public GetSubmissionsRequestValidator
    (
        EntityExistsValidator<Data.Challenge> challengeExists,
        TeamExistsValidator teamExists,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeExists = challengeExists;
        _teamExists = teamExists;
    }

    public async Task<GameboardAggregatedValidationExceptions> ValidateRequest(GetSubmissionsQuery query)
    {
        var validationExceptions = new List<GameboardValidationException>();

        var teamExistsResult = await _teamExists.Validate(query.teamId);
        if (teamExistsResult != null)
            validationExceptions.Add(teamExistsResult);

        var challengeExistsREsult = await _challengeExists.Validate(query.challengeId);
        if (challengeExistsREsult != null)
            validationExceptions.Add(challengeExistsREsult);

        return new GameboardAggregatedValidationExceptions(validationExceptions);
    }
}
