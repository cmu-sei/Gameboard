// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;

namespace Gameboard.Api.Services;

public class TicketMapper : Profile
{
    public JsonSerializerOptions JsonOptions { get; }

    public TicketMapper()
    {
        JsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        JsonOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );

        CreateMap<string[], string>()
            .ConvertUsing(arr => JsonSerializer.Serialize(arr, JsonOptions));

        CreateMap<Data.Ticket, Ticket>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s =>
                JsonSerializer.Deserialize<string[]>(s.Attachments, JsonOptions))
            );

        CreateMap<Data.User, TicketUser>()
            .ForMember(d => d.IsSupportPersonnel, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.ApprovedName ?? s.Name));

        CreateMap<Ticket, Data.Ticket>();
        CreateMap<Ticket, TicketSummary>();
        CreateMap<Data.Ticket, TicketSummary>()
            .AfterMap((s, d) =>
            {
                d.Assignee.Name = s.Assignee?.ApprovedName;
                d.Creator.Name = s.Creator?.ApprovedName;
                d.Requester.Name = s.Requester?.ApprovedName;
            });

        CreateMap<NewTicket, SelfNewTicket>();

        CreateMap<NewTicket, Data.Ticket>();
        CreateMap<NewTicket, SelfNewTicket>();
        CreateMap<SelfNewTicket, Data.Ticket>();
        CreateMap<ChangedTicket, Data.Ticket>();
        CreateMap<ChangedTicket, SelfChangedTicket>();
        CreateMap<SelfChangedTicket, Data.Ticket>();

        CreateMap<Data.TicketActivity, TicketActivity>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s => JsonSerializer.Deserialize<string[]>(s.Attachments, JsonOptions)))
            .ForMember(d => d.User, opt => opt.MapFrom(s => s.User))
        ;

        CreateMap<Ticket, TicketNotification>();
        CreateMap<TicketActivity, TicketNotification>();
    }
}
