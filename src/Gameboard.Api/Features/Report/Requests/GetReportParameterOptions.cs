using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetReportParameterOptionsQuery(string ReportKey, ReportParameters ReportParams) : IRequest<ReportParameterOptions>;

internal class GetReportParameterOptionsHandler : IRequestHandler<GetReportParameterOptionsQuery, ReportParameterOptions>
{
    private readonly IChallengeStore _challengeService;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly IPlayerStore _playerStore;
    private readonly ISponsorStore _sponsorStore;

    public GetReportParameterOptionsHandler
    (
        IChallengeStore challengeService,
        IGameStore gameStore,
        IMapper mapper,
        IPlayerStore playerStore,
        ISponsorStore sponsorStore
    )
    {
        _challengeService = challengeService;
        _gameStore = gameStore;
        _mapper = mapper;
        _playerStore = playerStore;
        _sponsorStore = sponsorStore;
    }

    public async Task<ReportParameterOptions> Handle(GetReportParameterOptionsQuery request, CancellationToken cancellationToken)
    {
        // TODO: validation
        // also, eventually reportKey might matter

        var hasChallenge = !string.IsNullOrWhiteSpace(request.ReportParams.ChallengeId);
        var hasGame = !string.IsNullOrWhiteSpace(request.ReportParams.GameId);
        var hasCompetition = !string.IsNullOrWhiteSpace(request.ReportParams.Competition);
        var hasTrack = !string.IsNullOrEmpty(request.ReportParams.Track);

        var hasPlayerNarrowingParameter = hasGame || hasChallenge || hasCompetition || hasTrack;

        var games = await _gameStore
            .List()
            .AsNoTracking()
            .Select(g => new
            {
                Id = g.Id,
                Name = g.Name,
                Competition = g.Competition,
                Track = g.Track
            }).ToArrayAsync();

        var challengesQuery = _challengeService
            .List()
            .AsNoTracking()
            .Where(c => !hasGame || c.GameId == request.ReportParams.GameId)
            .Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })
            .ToListAsync();

        // we don't let them filter on team or player unless they have a game or challenge selected because
        // of the sheer number of records

        var players = new Data.Player[] { };

        if (hasPlayerNarrowingParameter)
        {
            players = await _playerStore
                .List()
                .AsNoTracking()
                .Where
                (
                    p =>
                        (!hasGame || p.GameId == request.ReportParams.GameId) &&
                        (!hasChallenge || p.Challenges.Any(c => c.Id == request.ReportParams.ChallengeId)) &&
                        (!hasCompetition || p.Game.Competition == request.ReportParams.Competition) &&
                        (!hasTrack || p.Game.Track == request.ReportParams.Track)
                )
                .ToArrayAsync();
        }

        return new ReportParameterOptions
        {
            Challenges = await challengesQuery,
            Competitions = games
                .Select(g => g.Competition)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c),
            Games = games
                .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
                .OrderBy(g => g.Name),
            Players = players
                .Select(p => new SimpleEntity { Id = p.Id, Name = p.ApprovedName })
                .OrderBy(p => p.Name),
            Sponsors = await _sponsorStore
                .List()
                .AsNoTracking()
                .Select(s => new SimpleEntity { Id = s.Id, Name = s.Name })
                .OrderBy(s => s.Name)
                .ToArrayAsync(),
            Teams = players
                .Select(p => new SimpleEntity { Id = p.TeamId, Name = p.ApprovedName })
                .DistinctBy(t => t.Id)
                .OrderBy(p => p.Name),
            Tracks = games
                .Select(g => g.Track).Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t)
        };
    }
}
