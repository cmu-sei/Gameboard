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
        CreateMap<string[], string>()
            .ConvertUsing(arr => JsonSerializer.Serialize(arr, JsonOptions));

        CreateMap<Data.Ticket, Ticket>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s =>
                JsonSerializer.Deserialize<string[]>(s.Attachments, JsonOptions))
            );

        CreateMap<Ticket, Data.Ticket>();
        CreateMap<Ticket, TicketSummary>();
        CreateMap<Data.Ticket, TicketSummary>();

        CreateMap<NewTicket, SelfNewTicket>();

        CreateMap<NewTicket, Data.Ticket>();
        CreateMap<NewTicket, SelfNewTicket>();
        CreateMap<SelfNewTicket, Data.Ticket>();
        CreateMap<ChangedTicket, Data.Ticket>();
        CreateMap<ChangedTicket, SelfChangedTicket>();
        CreateMap<SelfChangedTicket, Data.Ticket>();

        CreateMap<Data.TicketActivity, TicketActivity>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s =>
                JsonSerializer.Deserialize<string[]>(s.Attachments, JsonOptions))
            )
        ;

        CreateMap<Ticket, TicketNotification>();
        CreateMap<TicketActivity, TicketNotification>();

        JsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        JsonOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    }
}
