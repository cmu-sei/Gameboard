// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Services
{
    public class ChallengeMapper : Profile
    {
        private JsonSerializerOptions JsonOptions { get; }

        public ChallengeMapper()
        {
            JsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.Challenge, TeamChallenge>();
            CreateMap<Data.Challenge, ChallengeOverview>()
                .ForMember(d => d.Score, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
                .ForMember(d => d.GameId, opt => opt.MapFrom(s => s.GameId))
                .ForMember(d => d.AllowTeam, opt => opt.MapFrom(s => s.Game.AllowTeam));

            CreateMap<Challenge, Data.Challenge>();
            CreateMap<Challenge, GameStartStateChallenge>()
                .ForMember(d => d.Challenge, o => o.MapFrom(s => new SimpleEntity { Id = s.Id, Name = s.Name }));
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
                    JsonSerializer.Deserialize<GameEngineGameState>(s.State, JsonOptions))
                )
            ;

            CreateMap<Data.Player, ChallengePlayer>()
                .ForMember(cp => cp.IsManager, o => o.MapFrom(p => p.Role == PlayerRole.Manager));

            CreateMap<Data.Challenge, ChallengeSummary>()
                .ForMember(d => d.Score, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
                .ForMember(d => d.Events, o => o.MapFrom(c => c.Events.OrderBy(e => e.Timestamp)))
                .ForMember(s => s.Players, o => o.MapFrom(d => new ChallengePlayer[]
                {
                    new ChallengePlayer
                    {
                        Id = d.PlayerId,
                        Name = d.Player.Name,
                        IsManager = d.Player.IsManager,
                        UserId = d.Player.UserId
                    }
                }))
                .ForMember(d => d.IsActive, opt => opt.MapFrom
                (
                    s => JsonSerializer.Deserialize<TopoMojo.Api.Client.GameState>(s.State, JsonOptions).IsActive)
                )
            ;

            CreateMap<Data.Challenge, ArchivedChallenge>()
                .ForMember(d => d.PlayerName, opt => opt.MapFrom(s => s.Player.ApprovedName))
                .ForMember(d => d.GameName, opt => opt.MapFrom(s => s.Game.Name))
                .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Player.UserId))
                .ForMember(d => d.Score, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
            ;

            // Squash arrays of challenge events, submissions, and team members into a single record
            CreateMap<ArchivedChallenge, Data.ArchivedChallenge>()
                .ForMember(d => d.Events, opt => opt.MapFrom(s =>
                    JsonSerializer.Serialize(s.Events, JsonOptions))
                )
                .ForMember(d => d.Submissions, opt => opt.MapFrom(s =>
                    JsonSerializer.Serialize(s.Submissions, JsonOptions))
                )
                .ForMember(d => d.TeamMembers, opt => opt.MapFrom(s =>
                    JsonSerializer.Serialize(s.TeamMembers, JsonOptions))
                );

            CreateMap<Data.ArchivedChallenge, ArchivedChallenge>()
                .ForMember(d => d.Events, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<ChallengeEventSummary[]>(s.Events, JsonOptions))
                )
                .ForMember(d => d.Submissions, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<GameEngineSectionSubmission[]>(s.Submissions, JsonOptions))
                )
                .ForMember(d => d.TeamMembers, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<string[]>(s.TeamMembers, JsonOptions))
                )
            ;

            CreateMap<Data.Challenge, ObserveChallenge>()
                .ForMember(d => d.GameRank, opt => opt.MapFrom(s => s.Player.Rank))
                .ForMember(d => d.GameScore, opt => opt.MapFrom(s => s.Player.Score))
                .ForMember(d => d.ChallengeScore, opt => opt.MapFrom(s => (int)Math.Floor(s.Score)))
                .ForMember(d => d.Consoles, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<TopoMojo.Api.Client.GameState>(s.State, JsonOptions).Vms)
                )
                .ForMember(d => d.isActive, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<TopoMojo.Api.Client.GameState>(s.State, JsonOptions).IsActive)
                )
            ;

            CreateMap<TopoMojo.Api.Client.VmState, ObserveVM>()
                 .ForMember(d => d.ChallengeId, opt => opt.MapFrom(s => s.IsolationId))
            ;

            CreateMap<TopoMojo.Api.Client.VmConsole, ConsoleSummary>()
                .ForMember(d => d.SessionId, opt => opt.MapFrom(s => s.IsolationId))
            ;

            CreateMap<Data.ChallengeEvent, ChallengeEventSummary>();
        }
    }
}
