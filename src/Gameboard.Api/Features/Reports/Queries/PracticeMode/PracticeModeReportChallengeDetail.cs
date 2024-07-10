using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Reports;

public sealed record PracticeModeReportChallengeDetailQuery(string ChallengeSpecId, PracticeModeReportParameters Parameters, PracticeModeReportChallengeDetailParameters ChallengeDetailParameters, PagingArgs PagingArgs) : IReportQuery, IRequest<PracticeModeReportChallengeDetail>;

internal sealed class PracticeModeReportChallengeDetailHandler : IRequestHandler<PracticeModeReportChallengeDetailQuery, PracticeModeReportChallengeDetail>
{
    private readonly IPagingService _pagingService;
    private readonly ReportsQueryValidator _reportsQueryValidator;
    private readonly IPracticeModeReportService _reportService;
    private readonly EntityExistsValidator<PracticeModeReportChallengeDetailQuery, Data.ChallengeSpec> _specExists;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly IValidatorService<PracticeModeReportChallengeDetailQuery> _validatorService;

    public PracticeModeReportChallengeDetailHandler
    (
        IPagingService pagingService,
        ReportsQueryValidator reportsQueryValidator,
        IPracticeModeReportService reportService,
        EntityExistsValidator<PracticeModeReportChallengeDetailQuery, Data.ChallengeSpec> specExists,
        IScoringService scoringService,
        IStore store,
        IValidatorService<PracticeModeReportChallengeDetailQuery> validatorService
    ) =>
    (
        _pagingService,
        _reportsQueryValidator,
        _reportService,
        _scoringService,
        _specExists,
        _store,
        _validatorService
    ) = (pagingService, reportsQueryValidator, reportService, scoringService, specExists, store, validatorService);


    public async Task<PracticeModeReportChallengeDetail> Handle(PracticeModeReportChallengeDetailQuery request, CancellationToken cancellationToken)
    {
        // validation
        await _reportsQueryValidator.Validate(request, cancellationToken);
        await _validatorService.AddValidator(_specExists.UseProperty(r => r.ChallengeSpecId)).Validate(request, cancellationToken);

        var specData = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Select(s => new
            {
                Game = new SimpleEntity { Id = s.Game.Id, Name = s.Game.Name },
                Spec = new SimpleEntity { Id = s.Id, Name = s.Name },
                MaxPossibleScore = s.Points
            })
            .SingleAsync(s => s.Spec.Id == request.ChallengeSpecId, cancellationToken);

        var query = await _reportService.GetBaseQuery(request.Parameters, false, cancellationToken);

        var results = await query
            .Where(c => c.SpecId == request.ChallengeSpecId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Player.UserId,
                c.Points,
                c.Score,
                c.Player.User.Sponsor,
                Duration = c.EndTime - c.StartTime,
                c.StartTime,
                UserName = c.Player.User.Name
            })
            .GroupBy(c => new
            {
                c.UserId,
                c.UserName,
                SponsorId = c.Sponsor.Id,
                SponsorName = c.Sponsor.Name,
                SponsorLogo = c.Sponsor.Logo
            })
            .ToDictionaryAsync(gr => gr.Key, gr => gr.OrderByDescending(c => c.Score).ToArray(), cancellationToken);

        // filter by solve type where requested
        if (request.ChallengeDetailParameters?.PlayersWithSolveType is not null)
        {
            switch (request.ChallengeDetailParameters.PlayersWithSolveType)
            {
                case ChallengeResult.Success:
                    results = results
                        .Where(kv => kv.Value.Any(c => c.Score >= c.Points))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    break;
                case ChallengeResult.Partial:
                    results = results
                        .Where(kv => kv.Value.All(c => c.Score < c.Points) && kv.Value.Any(c => c.Score > 0))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    break;
                case ChallengeResult.None:
                    results = results
                        .Where(kv => kv.Value.All(c => c.Score == 0))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    break;
            }
        }

        var pagingResults = _pagingService.Page(results, request.PagingArgs);

        var users = pagingResults.Items.Select(u => new PracticeModeReportChallengeDetailUser
        {
            User = new PlayerWithSponsor
            {
                Id = u.Key.UserId,
                Name = u.Key.UserName,
                Sponsor = new SimpleSponsor
                {
                    Id = u.Key.SponsorId,
                    Name = u.Key.SponsorName,
                    Logo = u.Key.SponsorLogo
                }
            },
            Sponsor = new SimpleSponsor
            {
                Id = u.Key.SponsorId,
                Name = u.Key.SponsorName,
                Logo = u.Key.SponsorLogo
            },
            AttemptCount = u.Value.Length,
            LastAttemptDate = u.Value.OrderByDescending(c => c.StartTime).First().StartTime,
            BestAttemptDate = u.Value.First().StartTime,
            BestAttemptDurationMs = u.Value.First().Duration.TotalMilliseconds,
            BestAttemptResult = _scoringService.GetChallengeResult(u.Value.First().Score, specData.MaxPossibleScore),
            BestAttemptScore = u.Value.First().Score
        });

        return new PracticeModeReportChallengeDetail
        {
            Game = specData.Game,
            Spec = specData.Spec,
            Users = users,
            Paging = pagingResults.Paging
        };
    }
}
