using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface ISupportReportService
{
    SupportReportStatSummary GetStatSummary(IEnumerable<SupportReportRecord> records);
    SupportReportTicketWindow GetTicketDateSupportWindow(DateTimeOffset ticketDate);
    Task<IEnumerable<SupportReportRecord>> QueryRecords(SupportReportParameters parameters);
}

internal class SupportReportService : ISupportReportService
{
    private readonly IJsonService _jsonService;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IReportsService _reportsService;
    private readonly TicketService _ticketService;
    private readonly ITicketStore _ticketStore;

    public SupportReportService
    (
        IJsonService jsonService,
        IMapper mapper,
        INowService now,
        IReportsService reportsService,
        TicketService ticketService,
        ITicketStore ticketStore
    )
    {
        _jsonService = jsonService;
        _mapper = mapper;
        _now = now;
        _reportsService = reportsService;
        _ticketService = ticketService;
        _ticketStore = ticketStore;
    }

    public SupportReportStatSummary GetStatSummary(IEnumerable<SupportReportRecord> records)
    {
        if (records is null || !records.Any())
        {
            return new SupportReportStatSummary
            {
                AllTicketsCount = 0,
                AllTicketsMostPopularLabel = null,
                OpenTicketsCount = 0,
                OpenTicketsMostPopularLabel = null,
                ChallengeSpecWithMostTickets = null
            };
        }

        var totalLabelCounts = new Dictionary<string, int>();
        var openLabelCounts = new Dictionary<string, int>();
        var openTicketCount = 0;
        var challengeSpecMap = new Dictionary<string, SupportReportStatSummaryChallengeSpec>();

        foreach (var record in records)
        {
            foreach (var label in record.Labels)
            {
                totalLabelCounts.EnsureKey(label, 0);
                totalLabelCounts[label] += 1;

                if (record.Status.ToLower() != "closed")
                {
                    openLabelCounts.EnsureKey(label, 0);
                    openLabelCounts[label] += 1;
                    openTicketCount += 1;
                }
            }

            if (record.Challenge is not null && record.ChallengeSpecId.IsNotEmpty())
            {
                challengeSpecMap.EnsureKey(record.ChallengeSpecId, new SupportReportStatSummaryChallengeSpec
                {
                    Id = record.Challenge.Id,
                    Name = record.Challenge.Name,
                    TicketCount = 0
                });

                challengeSpecMap[record.ChallengeSpecId].TicketCount += 1;
            }
        }

        var mostPopularLabel = totalLabelCounts
            .Select(l => (KeyValuePair<string, int>?)l)
            .OrderByDescending(k => k.Value.Value)
            .FirstOrDefault();

        var openMostPopularLabel = openLabelCounts
            .Select(l => (KeyValuePair<string, int>?)l)
            .OrderByDescending(k => k.Value.Value)
            .FirstOrDefault();

        return new SupportReportStatSummary
        {
            AllTicketsCount = records.Count(),
            AllTicketsMostPopularLabel = mostPopularLabel is null ? null : new SupportReportStatSummaryLabel
            {
                Label = mostPopularLabel.Value.Key,
                TicketCount = mostPopularLabel.Value.Value
            },
            OpenTicketsCount = openTicketCount,
            OpenTicketsMostPopularLabel = openMostPopularLabel is null ? null : new SupportReportStatSummaryLabel
            {
                Label = openMostPopularLabel.Value.Key,
                TicketCount = openMostPopularLabel.Value.Value
            },
            ChallengeSpecWithMostTickets = !challengeSpecMap.Any() ? null : challengeSpecMap.OrderByDescending(k => k.Value.TicketCount).First().Value
        };
    }

    public async Task<IEnumerable<SupportReportRecord>> QueryRecords(SupportReportParameters parameters)
    {
        // format parameters
        DateTimeOffset? openedDateStart = parameters.OpenedDateStart.HasValue ? parameters.OpenedDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? openedDateEnd = parameters.OpenedDateEnd.HasValue ? parameters.OpenedDateEnd.Value.ToEndDate().ToUniversalTime() : null;
        DateTimeOffset? updatedDateStart = parameters.UpdatedDateStart.HasValue ? parameters.UpdatedDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? updatedDateEnd = parameters.UpdatedDateEnd.HasValue ? parameters.UpdatedDateEnd.Value.ToEndDate().ToUniversalTime() : null;

        var labels = _reportsService.ParseMultiSelectCriteria(parameters.Labels);
        var statuses = _reportsService.ParseMultiSelectCriteria(parameters.Statuses);

        var query = _ticketStore
            .ListWithNoTracking()
            .AsSplitQuery()
            .Include(t => t.Assignee)
            .Include(t => t.Challenge)
            .Include(t => t.Creator)
            .Include(t => t.Player)
                .ThenInclude(p => p.Game)
            .Include(t => t.Requester)
            .Include(t => t.Activity.OrderBy(a => a.Timestamp))
            .Where(t => true);

        if (parameters.ChallengeSpecId.NotEmpty())
            query = query.Where(t => t.Challenge != null && t.Challenge.SpecId == parameters.ChallengeSpecId);

        if (parameters.GameId.NotEmpty())
            query = query.Where(t => t.Player != null && t.Player.GameId == parameters.GameId);

        if (openedDateStart is not null)
            query = query
                .Where(t => t.Created >= openedDateStart);

        if (openedDateEnd != null)
            query = query
                .Where(t => t.Created <= openedDateEnd);

        if (updatedDateStart is not null)
            query = query.Where(t => t.LastUpdated >= updatedDateStart);

        if (updatedDateEnd is not null)
            query = query.Where(t => t.LastUpdated <= updatedDateEnd);

        var rightNow = _now.Get();
        if (parameters.MinutesSinceOpen is not null)
        {
            var openSince = rightNow.Subtract(TimeSpan.FromMinutes(parameters.MinutesSinceOpen.Value));
            query = query.Where(t => t.Created <= openSince);
        }

        if (parameters.MinutesSinceUpdate is not null)
        {
            var notUpdatedSince = rightNow.Subtract(TimeSpan.FromMinutes(parameters.MinutesSinceUpdate.Value));
            query = query
                .Where(t => t.LastUpdated <= notUpdatedSince);
        }

        if (statuses.IsNotEmpty())
            query = query.Where(t => statuses.Contains(t.Status.ToLower()));

        var results = await query
            .OrderBy(t => t.Created)
            .ToListAsync();

        // client side processing
        IEnumerable<SupportReportRecord> records = results.Select(t => new SupportReportRecord
        {
            Key = t.Key,
            PrefixedKey = _ticketService.TransformTicketKey(t.Key),
            CreatedOn = t.Created,
            UpdatedOn = t.LastUpdated,
            Summary = t.Summary,
            Status = t.Status,
            AssignedTo = _mapper.Map<SimpleEntity>(t.Assignee),
            CreatedBy = _mapper.Map<SimpleEntity>(t.Creator),
            UpdatedBy = t.Activity.Count() > 0 ? _mapper.Map<SimpleEntity>(t.Activity.OrderBy(a => a.Timestamp).Last().User) : null,
            RequestedBy = _mapper.Map<SimpleEntity>(t.Requester),
            Game = t.Player == null ? null : _mapper.Map<SimpleEntity>(t.Player.Game),
            Challenge = _mapper.Map<SimpleEntity>(t.Challenge),
            ChallengeSpecId = t.Challenge?.SpecId,
            AttachmentUris = _jsonService.Deserialize<List<string>>(t.Attachments),
            Labels = _ticketService.TransformTicketLabels(t.Label),
            ActivityCount = t.Activity.Count()
        });

        // we have to do labels in .net land, because they're stored as space-delimited values in a single column
        if (labels.IsNotEmpty())
            records = records.Where(r => labels.Any(l => r.Labels.Any(r => r == l)));

        if (parameters.OpenedWindow != null)
            records = records.Where(r => GetTicketDateSupportWindow(r.CreatedOn) == parameters.OpenedWindow);

        return records;
    }

    public SupportReportTicketWindow GetTicketDateSupportWindow(DateTimeOffset ticketDate)
    {
        var localizedHours = ticketDate.ToLocalTime().Hour;

        if (localizedHours < 8)
            return SupportReportTicketWindow.OffHours;

        if (localizedHours < 17)
            return SupportReportTicketWindow.BusinessHours;

        return SupportReportTicketWindow.EveningHours;
    }
}
