using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Common;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface ISupportReportService
{
    SupportReportTicketWindow GetTicketDateSupportWindow(DateTimeOffset ticketDate);
    Task<IEnumerable<SupportReportRecord>> QueryRecords(SupportReportParameters parameters);
}

internal class SupportReportService : ISupportReportService
{
    private static readonly string LIST_PARAMETER_DELIMITER = ",";

    private readonly IJsonService _jsonService;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly TicketService _ticketService;
    private readonly ITicketStore _ticketStore;

    public SupportReportService
    (
        IJsonService jsonService,
        IMapper mapper,
        INowService now,
        TicketService ticketService,
        ITicketStore ticketStore
    )
    {
        _jsonService = jsonService;
        _mapper = mapper;
        _now = now;
        _ticketService = ticketService;
        _ticketStore = ticketStore;
    }

    public async Task<IEnumerable<SupportReportRecord>> QueryRecords(SupportReportParameters parameters)
    {
        var query = _ticketStore
            .ListWithNoTracking()
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

        if (parameters.OpenedDateStart != null)
            query = query.Where(t => t.Created >= parameters.OpenedDateStart);

        if (parameters.OpenedDateEnd != null)
            query = query.Where(t => t.Created <= parameters.OpenedDateEnd);

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
            UpdatedBy = t.Activity.Count() > 0 ? _mapper.Map<SimpleEntity>(t.Activity.OrderBy(a => a.Timestamp).Last().User) : null,
            RequestedBy = _mapper.Map<SimpleEntity>(t.Requester),
            Game = t.Player == null ? null : _mapper.Map<SimpleEntity>(t.Player.Game),
            Challenge = _mapper.Map<SimpleEntity>(t.Challenge),
            AttachmentUris = _jsonService.Deserialize<List<string>>(t.Attachments),
            Labels = _ticketService.TransformTicketLabels(t.Label),
            ActivityCount = t.Activity.Count()
        }).ToArrayAsync();

        if (parameters.Labels.NotEmpty())
        {
            var splits = parameters.Labels.Split(LIST_PARAMETER_DELIMITER, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters.LabelsModifier == null || parameters.LabelsModifier == SupportReportLabelsModifier.HasAll)
            {
                records = records.Where(r => r.Labels.Any() && splits.All(s => r.Labels.Contains(s)));
            }

            if (parameters.LabelsModifier == SupportReportLabelsModifier.HasAny)
            {
                // if (parameters.LabelsModifier == null || parameters.LabelsModifier == SupportReportLabelsModifier.HasAny)
                //     records = records.Where(r => r.Labels.Any(l => parameters.Labels.Contains(l)));
                throw new NotImplementedException();
            }
        }

        if (parameters.MinutesSinceOpen != null && parameters.MinutesSinceOpen > 0)
            records = records.Where(r => (_now.Get().Subtract(r.CreatedOn).TotalMinutes >= parameters.MinutesSinceOpen));

        if (parameters.MinutesSinceUpdate != null && parameters.MinutesSinceUpdate > 0)
            records = records.Where(r => (_now.Get().Subtract(r.UpdatedOn).TotalHours >= parameters.MinutesSinceUpdate));

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
