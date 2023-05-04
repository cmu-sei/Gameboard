using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetChallengeReportQuery(GetChallengesReportQueryArgs Args) : IRequest<ReportResults<ChallengeReportRecord>>;

public class GetChallengeReportHandler : IRequestHandler<GetChallengeReportQuery, ReportResults<ChallengeReportRecord>>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IReportsService _reportsService;

    public GetChallengeReportHandler
    (
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        IReportsService reportsService
    )
    {
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _reportsService = reportsService;
    }

    public async Task<ReportResults<ChallengeReportRecord>> Handle(GetChallengeReportQuery request, CancellationToken cancellationToken)
    {
        var results = await _reportsService.GetChallengesReportRecords(request.Args);

        return new ReportResults<ChallengeReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.ChallengesReport,
                Title = "Challenge Report",
                RunAt = _now.Get()
            },
            Records = results
        };
    }
}
