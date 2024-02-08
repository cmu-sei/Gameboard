using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

public sealed class GetExternalTeamDataResponse
{
    public required string TeamId { get; set; }
    public required ExternalGameTeamDeployStatus DeployStatus { get; set; }
    public required string ExternalUrl { get; set; }
}

public record GetExternalTeamDataQuery(string TeamId, User ActingUser) : IRequest<GetExternalTeamDataResponse>;

internal class GetExternalTeamDataQueryHandler : IRequestHandler<GetExternalTeamDataQuery, GetExternalTeamDataResponse>
{
    private readonly IExternalGameService _externalGameService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetExternalTeamDataQuery> _teamExists;
    private readonly IValidatorService<GetExternalTeamDataQuery> _validator;

    public GetExternalTeamDataQueryHandler
    (
        IExternalGameService externalGameService,
        IStore store,
        TeamExistsValidator<GetExternalTeamDataQuery> teamExists,
        IValidatorService<GetExternalTeamDataQuery> validator
    )
    {
        _externalGameService = externalGameService;
        _store = store;
        _teamExists = teamExists;
        _validator = validator;
    }

    public async Task<GetExternalTeamDataResponse> Handle(GetExternalTeamDataQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        _validator.AddValidator(async (req, ctx) =>
        {
            var userPlayer = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == request.TeamId && p.UserId == request.ActingUser.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (userPlayer is null)
                ctx.AddValidationException(new UserIsntOnTeam(request.ActingUser.Id, request.TeamId));
        });
        await _validator.Validate(request, cancellationToken);

        // get the metadata for this team's participation in the external game
        var teamData = await _externalGameService.GetTeam(request.TeamId, cancellationToken);

        if (teamData is null)
            throw new ResourceNotFound<ExternalGameTeam>(request.TeamId);

        return new GetExternalTeamDataResponse
        {
            DeployStatus = teamData.DeployStatus,
            ExternalUrl = teamData.ExternalGameUrl,
            TeamId = teamData.TeamId
        };
    }
}
