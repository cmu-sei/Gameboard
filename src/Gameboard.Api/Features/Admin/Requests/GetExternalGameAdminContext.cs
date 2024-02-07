using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetExternalGameAdminContextRequest(string GameId) : IRequest<ExternalGameAdminContext>;

internal class GetExternalGameAdminContextHandler : IRequestHandler<GetExternalGameAdminContextRequest, ExternalGameAdminContext>
{
    private readonly GameWithModeExistsValidator<GetExternalGameAdminContextRequest> _gameExists;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetExternalGameAdminContextRequest> _validator;

    public GetExternalGameAdminContextHandler
    (
        GameWithModeExistsValidator<GetExternalGameAdminContextRequest> gameExists,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetExternalGameAdminContextRequest> validator
    )
    {
        _gameExists = gameExists;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<ExternalGameAdminContext> Handle(GetExternalGameAdminContextRequest request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();

        _validator.AddValidator
        (
            _gameExists
                .UseIdProperty(r => r.GameId)
                .WithEngineMode(GameEngineMode.External)
                .WithSyncStartRequired(true)
        );

        await _validator.Validate(request, cancellationToken);

        // do the business
        var gameData = await _store
            .WithNoTracking<Data.Game>()
            .Include(g => g.Specs)
            .Include(g => g.Players)
                .ThenInclude(p => p.Sponsor)
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
            .SingleAsync(g => g.Id == request.GameId, cancellationToken);

        var specIds = gameData.Specs.Select(s => s.Id).ToArray();

        // get challenges separately because of SpecId nonsense
        // group by TeamId for quicker lookups
        var teamChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => specIds.Contains(c.SpecId))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.ToArray(), cancellationToken);

        // Compute hasStandardSessionWindow: do all players start and end at the same time
        // as all challenges?
        var startDates = teamChallenges
            .SelectMany(c => c.Value)
            .Select(c => c.StartTime)
            .Concat(gameData.Players.Select(p => p.SessionBegin))
            .Distinct()
            .ToArray();
        var endDates = teamChallenges
            .SelectMany(c => c.Value)
            .Select(c => c.EndTime)
            .Concat(gameData.Players.Select(p => p.SessionEnd))
            .Distinct()
            .ToArray();

        // this expresses whether there are any player session dates or
        // challenge start/end times that are misaligned - only really matters
        // once the game has started
        var hasStandardSessionWindow = startDates.Length > 1 && endDates.Length > 1;

        // if we have a standardized session window, send it down with the data
        DateTimeOffset? overallStart = null;
        DateTimeOffset? overallEnd = null;
        if (startDates.Length == 1 && endDates.Length == 1)
        {
            overallStart = startDates.Single();
            overallEnd = endDates.Single();
        }

        // compute teams
        var teams = gameData.Players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        var teamIds = teams.Select(entry => entry.Key).ToArray();

        var captains = teams.Keys
            .ToDictionary(key => key, key => _teamService.ResolveCaptain(teams[key]));

        var teamDeployStatuses = new Dictionary<string, ExternalGameTeamDeployStatus>();
        foreach (var teamId in teams.Keys)
        {
            if (!teamChallenges.ContainsKey(teamId) || !teamChallenges[teamId].Any())
            {
                teamDeployStatuses.Add(teamId, ExternalGameTeamDeployStatus.NotStarted);
                continue;
            }

            var allChallengesCreated = specIds.All(specId => teamChallenges[teamId].Any(c => c.SpecId == specId));
            var allGamespacesDeployedOrFinished = teamChallenges[teamId].All(c => c.HasDeployedGamespace || c.Score >= c.Points);

            if (allChallengesCreated && allGamespacesDeployedOrFinished)
            {
                teamDeployStatuses.Add(teamId, ExternalGameTeamDeployStatus.Deployed);
                continue;
            }

            teamDeployStatuses.Add(teamId, ExternalGameTeamDeployStatus.Deploying);
        }

        // and their readiness
        var syncStartState = await _syncStartGameService.GetSyncStartState(request.GameId, cancellationToken);
        var playerReadiness = syncStartState.Teams
            .SelectMany(t => t.Players)
            .ToDictionary(p => p.Id, p => p.IsReady);

        // the game's overall deploy status is the lowest value of all teams' deploy statuses
        var overallDeployState = ResolveOverallDeployStatus(teamDeployStatuses.Values);

        return new ExternalGameAdminContext
        {
            Game = new SimpleEntity { Id = gameData.Id, Name = gameData.Name },
            OverallDeployStatus = overallDeployState,
            Specs = gameData.Specs.Select(s => new SimpleEntity { Id = s.Id, Name = s.Name }),
            HasNonStandardSessionWindow = hasStandardSessionWindow,
            StartTime = overallStart,
            EndTime = overallEnd,
            Teams = teams.Keys.Select(key => new ExternalGameAdminTeam
            {
                Id = captains[key].TeamId,
                Name = captains[key].ApprovedName,
                DeployStatus = teamDeployStatuses.ContainsKey(key) ?
                    teamDeployStatuses[key] :
                    ExternalGameTeamDeployStatus.NotStarted,
                IsReady = teams[key].All(p => p.IsReady),
                Challenges = gameData.Specs.OrderBy(s => s.Name).Select(s =>
                    {
                        // note that the team may not have any challenges and thus not be
                        // in the challenge dictionary. If they aren't, use default
                        // values
                        string id = null;
                        var challengeCreated = false;
                        var gamespaceDeployed = false;
                        string specId = s.Id;
                        DateTimeOffset? startTime = null;
                        DateTimeOffset? endTime = null;

                        if (teamChallenges.ContainsKey(key))
                        {
                            var challenge = teamChallenges[key].SingleOrDefault(c => c.SpecId == s.Id);

                            id = challenge?.Id;
                            challengeCreated = challenge is not null;
                            gamespaceDeployed = challenge is not null & challenge.HasDeployedGamespace;
                            startTime = challenge?.StartTime;
                            endTime = challenge?.EndTime;
                        }

                        return new ExternalGameAdminChallenge
                        {
                            Id = id,
                            ChallengeCreated = challengeCreated,
                            GamespaceDeployed = gamespaceDeployed,
                            SpecId = specId,
                            StartTime = startTime,
                            EndTime = endTime
                        };
                    }),
                Players = teams[key].Select(p => new ExternalGameAdminPlayer
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsCaptain = captains[key].Id == p.Id,
                    Sponsor = new SimpleSponsor
                    {
                        Id = p.SponsorId,
                        Name = p.Sponsor.Name,
                        Logo = p.Sponsor.Logo
                    },
                    Status = playerReadiness[p.Id] ?
                        ExternalGameAdminPlayerStatus.Ready :
                        ExternalGameAdminPlayerStatus.NotReady,
                    User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName }
                }),
                Sponsors = teams[key].Select(p => new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }),
            })
        };
    }

    private ExternalGameAdminOverallDeployStatus ResolveOverallDeployStatus(IEnumerable<ExternalGameTeamDeployStatus> teamStatuses)
    {
        if (teamStatuses.All(s => s == ExternalGameTeamDeployStatus.Deployed))
            return ExternalGameAdminOverallDeployStatus.Deployed;

        if (teamStatuses.All(s => s == ExternalGameTeamDeployStatus.NotStarted))
            return ExternalGameAdminOverallDeployStatus.NotStarted;

        if (teamStatuses.Any(s => s == ExternalGameTeamDeployStatus.Deploying))
            return ExternalGameAdminOverallDeployStatus.Deploying;

        return ExternalGameAdminOverallDeployStatus.PartiallyDeployed;
    }
}
