using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public sealed record GetPlayersCsvExportQuery(string GameId, IEnumerable<string> TeamIds) : IRequest<GetPlayersCsvExportResponse>;

internal sealed class GetPlayersCsvExportHandler(
    IStore store,
    EntityExistsValidator<GetPlayersCsvExportQuery, Data.Game> gameExists,
    TeamExistsValidator<GetPlayersCsvExportQuery> teamsExist,
    IValidatorService<GetPlayersCsvExportQuery> validatorService
    ) : IRequestHandler<GetPlayersCsvExportQuery, GetPlayersCsvExportResponse>
{
    private readonly IStore _store = store;
    private readonly EntityExistsValidator<GetPlayersCsvExportQuery, Data.Game> _gameExists = gameExists;
    private readonly TeamExistsValidator<GetPlayersCsvExportQuery> _teamsExist = teamsExist;
    private readonly IValidatorService<GetPlayersCsvExportQuery> _validatorService = validatorService;

    public async Task<GetPlayersCsvExportResponse> Handle(GetPlayersCsvExportQuery request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _validatorService
            .Auth(c => c.Require(Users.PermissionKey.Admin_View))
            .AddValidator(_gameExists.UseProperty(r => r.GameId));

        var teamIds = request.TeamIds?.Where(t => t.IsNotEmpty()).Distinct().ToArray();
        if (teamIds is not null && teamIds.Any())
            _validatorService.AddValidator(_teamsExist.UseProperty(r => r.TeamIds));

        await _validatorService.Validate(request, cancellationToken);

        // and get that biz
        var teams = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(t => t.GameId == request.GameId)
            .Where(t => teamIds == null || teamIds.Contains(t.TeamId))
            .Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.Rank,
                t.ScoreOverall,
                t.SolveCountComplete,
                t.SolveCountPartial
            })
            .ToDictionaryAsync(t => t.TeamId, t => t, cancellationToken);

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == request.GameId)
            .Where(p => teamIds == null || teamIds.Contains(p.TeamId))
            .Where(p => p.Mode == PlayerMode.Competition)
            .Select(p => new
            {
                p.Id,
                Name = p.ApprovedName,
                Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Name },
                Session = new DateRange
                {
                    Start = p.SessionBegin,
                    End = p.SessionEnd
                },
                Rank = 0,
                p.TeamId,
                TimeMs = p.Time,
                User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName }
            }).ToArrayAsync(cancellationToken);

        return new GetPlayersCsvExportResponse
        {
            Players = players.Select(p =>
            {
                teams.TryGetValue(p.TeamId, out var team);

                return new GetPlayersCsvExportResponsePlayer
                {
                    Id = p.Id,
                    Name = p.Name,
                    Game = p.Game,
                    Rank = team?.Rank ?? default(int?),
                    Score = team?.ScoreOverall ?? null,
                    Session = p.Session,
                    SolvesCorrectCount = team?.SolveCountComplete ?? 0,
                    SolvesPartialCount = team?.SolveCountPartial ?? 0,
                    Team = team is null ? null : new SimpleEntity { Id = team.TeamId, Name = team.TeamName },
                    TimeMs = p.TimeMs > 0 ? p.TimeMs : null,
                    User = p.User
                };
            })
        };
    }
}
