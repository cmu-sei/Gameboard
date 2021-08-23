// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;


namespace Gameboard.Api.Services
{
    public class ChallengeMapper : Profile
    {
        public ChallengeMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());



            CreateMap<Data.Challenge, TeamChallenge>();

            CreateMap<Challenge, Data.Challenge>();

            CreateMap<NewChallenge, Data.Challenge>();

            CreateMap<ChangedChallenge, Data.Challenge>();

            CreateMap<Data.ChallengeSpec, Data.Challenge>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.SpecId, opt => opt.MapFrom(s => s.Id))
            ;

            CreateMap<Data.Player, Data.Challenge>();

            CreateMap<TopoMojo.Api.Client.GameState, Data.Challenge>()
                .ForMember(d => d.LastSyncTime, opt => opt.MapFrom(s => DateTimeOffset.UtcNow))
                .ForMember(d => d.LastScoreTime, opt => opt.MapFrom(s => s.Challenge.LastScoreTime))
                .ForMember(d => d.Score, opt => opt.MapFrom(s => s.Challenge.Score))
                .ForMember(d => d.HasDeployedGamespace, opt => opt.MapFrom(s => s.Vms.Count > 0))
                .ForMember(d => d.State, opt => opt.MapFrom(s =>
                    JsonSerializer.Serialize(s, JsonOptions))
                )
            ;

            CreateMap<Data.Challenge, Challenge>()
                .ForMember(d => d.Score, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
                .ForMember(d => d.State, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<TopoMojo.Api.Client.GameState>(s.State, JsonOptions))
                )
            ;

            CreateMap<Data.Challenge, ChallengeSummary>()
                .ForMember(d => d.Score, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
            ;

            CreateMap<TopoMojo.Api.Client.VmConsole, ConsoleSummary>()
                .ForMember(d => d.SessionId, opt => opt.MapFrom(s => s.IsolationId))
            ;
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

        public JsonSerializerOptions JsonOptions { get; }
    }
}
