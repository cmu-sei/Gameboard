using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record SupportReportQuery(SupportReportQueryParameters Parameters) : IRequest<ReportResults<SupportReportRecord>>;

internal class SupportReportQueryHandler : IRequestHandler<SupportReportQuery, ReportResults<SupportReportRecord>>
{
    private readonly IJsonService _jsonService;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly CoreOptions _options;
    private readonly ITicketStore _ticketStore;
    private readonly TicketService _ticketService;

    public SupportReportQueryHandler
    (
        IJsonService jsonService,
        IMapper mapper,
        INowService now,
        CoreOptions options,
        ITicketStore ticketStore,
        TicketService ticketService
    )
    {
        _jsonService = jsonService;
        _mapper = mapper;
        _now = now;
        _options = options;
        _ticketStore = ticketStore;
        _ticketService = ticketService;
    }

    public async Task<ReportResults<SupportReportRecord>> Handle(SupportReportQuery request, CancellationToken cancellationToken)
    {
        return new ReportResults<SupportReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Support Report",
                RunAt = _now.Get(),
                Key = ReportKey.SupportReport
            },
            Records = await QueryRecords(request.Parameters)
        };
    }

    internal async Task<IEnumerable<SupportReportRecord>> QueryRecords(SupportReportQueryParameters parameters)
    {
        var query = _ticketStore
            .ListWithNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.Challenge)
            .Include(t => t.Creator)
            .Include(t => t.Player)
                .ThenInclude(p => p.Game)
            .Include(t => t.Requester)
            .Include(t => t.Activity)
            .Where(t => true);

        if (parameters.ChallengeSpecId.NotEmpty())
            query = query.Where(t => t.Challenge.SpecId == parameters.ChallengeSpecId);

        if (parameters.GameId.NotEmpty())
            query = query.Where(t => t.Player.GameId == parameters.GameId);

        if (parameters.OpenedDateRange?.DateStart != null)
            query = query.Where(t => t.Created >= parameters.OpenedDateRange.DateStart);

        if (parameters.OpenedDateRange?.DateEnd != null)
            query = query.Where(t => t.Created >= parameters.OpenedDateRange.DateEnd);

        if (parameters.Status.NotEmpty())
            query = query.Where(t => t.Status == parameters.Status);

        // client side processing
        IEnumerable<SupportReportRecord> records = await query.Select(t => new SupportReportRecord
        {
            Key = t.Key,
            PrefixedKey = _ticketService.TransformTicketKey(t.Key),
            CreatedOn = t.Created,
            UpdatedOn = t.LastUpdated,
            Summary = t.Summary,
            Status = t.Status,
            AssignedTo = _mapper.Map<SimpleEntity>(t.Assignee),
            CreatedBy = _mapper.Map<SimpleEntity>(t.Creator),
            RequestedBy = _mapper.Map<SimpleEntity>(t.Requester),
            Game = _mapper.Map<SimpleEntity>(t.Player.Game),
            Challenge = _mapper.Map<SimpleEntity>(t.Challenge),
            AttachmentUris = _jsonService.Deserialize<List<string>>(t.Attachments),
            Labels = _ticketService.TransformTicketLabels(t.Label),
            ActivityCount = t.Activity.Count()
        }).ToArrayAsync();

        if (parameters.Labels != null && parameters.Labels.Labels?.Count() > 0)
        {
            if (parameters.Labels.Modifier == SupportReportLabelsModifier.HasAny)
                records = records.Where(r => r.Labels.Any(l => parameters.Labels.Labels.Contains(l)));

            if (parameters.Labels.Modifier == SupportReportLabelsModifier.HasAll)
                records = records.Where(r => r.Labels.All(l => parameters.Labels.Labels.Contains(l)));
        }

        if (parameters.HoursSinceOpen != null)
            records = records.Where(r => (_now.Get().Subtract(r.CreatedOn).TotalHours >= parameters.HoursSinceOpen));

        if (parameters.HoursSinceStatusChange != null)
            records = records.Where(r => (_now.Get().Subtract(r.UpdatedOn).TotalHours >= parameters.HoursSinceOpen));

        if (parameters.OpenedWindow != null)
            records = records.Where(r => GetDateTimeSupportWindow(r.CreatedOn) == parameters.OpenedWindow);

        return records;
    }

    internal SupportReportTicketWindow GetDateTimeSupportWindow(DateTimeOffset dateTime)
    {
        var localizedHours = dateTime.ToLocalTime().Hour;

        if (localizedHours < 8)
            return SupportReportTicketWindow.OffHours;

        if (localizedHours < 17)
            return SupportReportTicketWindow.BusinessHours;

        return SupportReportTicketWindow.EveningHours;
    }
}
