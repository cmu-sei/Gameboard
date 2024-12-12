using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IFeedbackReportService
{
    Task<IEnumerable<FeedbackReportRecord>> GetBaseQuery(FeedbackReportParameters parameters, CancellationToken cancellationToken);
}

internal sealed class FeedbackReportService(IReportsService reportsService, IStore store) : IFeedbackReportService
{
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

    public async Task<IEnumerable<FeedbackReportRecord>> GetBaseQuery(FeedbackReportParameters parameters, CancellationToken cancellationToken)
    {
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var sponsorsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var tracksCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);
        var submissionDateStart = parameters.SubmissionDateStart.HasValue ? parameters.SubmissionDateStart.Value.ToUniversalTime() : default(DateTimeOffset?);
        var submissionDateEnd = parameters.SubmissionDateEnd.HasValue ? parameters.SubmissionDateEnd.Value.ToEndDate().ToUniversalTime() : default(DateTimeOffset?);

        var matchingSubmissions = _store
            .WithNoTracking<FeedbackSubmission>()
            .Include(s => s.User)
            .Where(s => s.FeedbackTemplateId == parameters.TemplateId);

        if (submissionDateStart.IsNotEmpty())
        {
            matchingSubmissions = matchingSubmissions.Where(s => s.WhenCreated >= submissionDateStart.Value);
        }

        if (submissionDateEnd.IsNotEmpty())
        {
            matchingSubmissions = matchingSubmissions.Where(s => s.WhenCreated <= submissionDateEnd.Value);
        }

        if (sponsorsCriteria.Any())
        {
            matchingSubmissions = matchingSubmissions.Where(s => sponsorsCriteria.Contains(s.User.SponsorId));
        }

        IEnumerable<FeedbackReportRecord> records = await matchingSubmissions
            .Select(s =>
                new FeedbackReportRecord
                {
                    Id = s.Id,
                    Entity = new FeedbackSubmissionAttachedEntity
                    {
                        Id = s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                        ((FeedbackSubmissionChallengeSpec)s).ChallengeSpecId :
                        ((FeedbackSubmissionGame)s).GameId,
                        EntityType = s.AttachedEntityType
                    },
                    ChallengeSpec = s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ? new SimpleEntity
                    {
                        Id = ((FeedbackSubmissionChallengeSpec)s).ChallengeSpecId,
                        Name = ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Name
                    } : null,
                    // wonky casting syntax is required because EF query translation
                    LogicalGame = new FeedbackReportRecordGame
                    {
                        Id = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Id,
                        Name = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Name,
                        Division = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Division,
                        Season = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Season,
                        Series = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Competition,
                        Track = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).Track,
                        IsTeamGame = (
                            s.AttachedEntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec ?
                            ((FeedbackSubmissionChallengeSpec)s).ChallengeSpec.Game :
                            ((FeedbackSubmissionGame)s).Game
                        ).MinTeamSize > 1
                    },
                    Sponsor = new ReportSponsorViewModel
                    {
                        Id = s.User.SponsorId,
                        LogoFileName = s.User.Sponsor.Logo,
                        Name = s.User.Sponsor.Name
                    },
                    Responses = s.Responses,
                    User = new SimpleEntity { Id = s.UserId, Name = s.User.ApprovedName },
                    WhenCreated = s.WhenCreated,
                    WhenEdited = s.WhenEdited,
                    WhenFinalized = s.WhenFinalized,
                }).ToArrayAsync(cancellationToken);

        // have to do these "client" side because of translation :(
        if (gamesCriteria.Any())
        {
            records = records.Where(s => gamesCriteria.Contains(s.LogicalGame.Id));
        }

        if (seasonsCriteria.Any())
        {
            records = records.Where(s => seasonsCriteria.Contains(s.LogicalGame.Season));
        }

        if (seriesCriteria.Any())
        {
            records = records.Where(s => seriesCriteria.Contains(s.LogicalGame.Series));
        }

        if (tracksCriteria.Any())
        {
            records = records.Where(s => tracksCriteria.Contains(s.LogicalGame.Track));
        }

        var orderedQuery = records.OrderBy(r => r.User.Name);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort)
            {
                case "game":
                    orderedQuery = orderedQuery
                        .Sort(r => r.LogicalGame.Name, parameters.SortDirection)
                        .ThenBy(r => r.ChallengeSpec.Name);
                    break;
                case "when-created":
                    orderedQuery = orderedQuery.Sort(r => r.WhenCreated, parameters.SortDirection);
                    break;
            }
        }

        return orderedQuery;
    }
}
