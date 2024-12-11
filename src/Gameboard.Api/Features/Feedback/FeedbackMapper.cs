// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api.Services
{
    public class FeedbackMapper : Profile
    {
        public FeedbackMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.Feedback, Feedback>()
                .ForMember(d => d.Questions, opt => opt.MapFrom(s =>
                    JsonSerializer.Deserialize<QuestionSubmission[]>(s.Answers, JsonOptions))
                );

            CreateMap<Feedback, Data.Feedback>();

            CreateMap<FeedbackSubmissionLegacy, Data.Feedback>()
                .ForMember(d => d.Submitted, opt => opt.MapFrom(s => s.Submit))
                .ForMember(d => d.Answers, opt => opt.MapFrom(s =>
                    JsonSerializer.Serialize(s.Questions, JsonOptions))
                );

            CreateMap<Data.Feedback, FeedbackReportDetails>()
                .ForMember(d => d.Questions, opt => opt.MapFrom(s =>
                        JsonSerializer.Deserialize<QuestionSubmission[]>(s.Answers, JsonOptions))
                )
                .ForMember(d => d.ApprovedName, opt => opt.MapFrom(s => s.Player.ApprovedName))
                .ForMember(d => d.ChallengeTag, opt => opt.MapFrom(s => s.ChallengeSpec.Tag));

            CreateMap<Data.FeedbackTemplate, FeedbackTemplateView>()
                .ForMember(d => d.CreatedBy, opt => opt.MapFrom(s => new SimpleEntity { Id = s.CreatedByUserId, Name = s.CreatedByUser.ApprovedName }))
                .ForMember(d => d.ResponseCount, opt => opt.MapFrom(s => s.Submissions.Count()))
                .ForMember(d => d.UseForGames, opt => opt.MapFrom
                (
                    s => s
                        .UseAsFeedbackTemplateForGames
                        .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
                        .ToArray()
                ))
                .ForMember(d => d.UseForGameChallenges, opt => opt.MapFrom
                (
                    s => s
                        .UseAsFeedbackTemplateForGameChallenges
                        .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
                        .ToArray()
                ));

            // Use a dictionary to map each question id in a response to the answer 
            CreateMap<FeedbackReportDetails, FeedbackReportHelper>()
                .AfterMap((src, dest) =>
                {
                    foreach (QuestionSubmission q in src.Questions) { dest.IdToAnswer.Add(q.Id, q.Answer); }
                });

            JsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            JsonOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        }

        public JsonSerializerOptions JsonOptions { get; }
    }
}
