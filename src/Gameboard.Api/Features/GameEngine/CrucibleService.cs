// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Alloy.Api.Client;
using System.Threading.Tasks;
using TopoMojo.Api.Client;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Services
{
    public class CrucibleService : _Service
    {
        IChallengeStore Store { get; }
        IAlloyApiClient Alloy { get; }
        CrucibleOptions CrucibleOptions { get; }
        ILockService LockService { get; }

        public CrucibleService(
            ILogger<ChallengeService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeStore store,
            IAlloyApiClient alloy,
            CrucibleOptions crucibleOptions,
            ILockService lockService
        ) : base(logger, mapper, options)
        {
            Store = store;
            Alloy = alloy;
            CrucibleOptions = crucibleOptions;
            LockService = lockService;
        }

        public async Task<GameState> RegisterGamespace(Data.ChallengeSpec spec, Data.Game game, Data.Player player, Data.Challenge entity)
        {
            var whenCreated = DateTimeOffset.UtcNow;
            var additionalUserIds = new List<Guid>();

            if (game.AllowTeam)
            {
                additionalUserIds = await Store.DbContext.Players
                    .Where(x => x.TeamId == player.TeamId && x.Id != player.Id)
                    .Select(x => new Guid(x.UserId))
                    .ToListAsync();
            }

            var evt = await Alloy.CreateEventFromEventTemplate2Async(new Guid(spec.ExternalId), new CreateEventCommand
            {
                AdditionalUserIds = additionalUserIds,
                UserId = new Guid(player.UserId),
                Username = player.ApprovedName
            });

            while (evt.Status != EventStatus.Active && evt.Status != EventStatus.Ended && evt.Status != EventStatus.Expired)
            {
                evt = await Alloy.GetEventAsync(evt.Id);
                await Task.Delay(1000);
            }

            var state = new GameState
            {
                Markdown = spec.Description,
                IsActive = true,
                Players = new List<TopoMojo.Api.Client.Player>
                    {
                        new TopoMojo.Api.Client.Player
                        {
                            GamespaceId = evt.Id.ToString(),
                            SubjectId = player.TeamId,
                            SubjectName = player.ApprovedName
                        }
                    },
                Vms = new List<VmState>(),
                Name = spec.Name,
                Id = entity.Id,
                WhenCreated = whenCreated,
                StartTime = DateTimeOffset.UtcNow,
                ExpirationTime = entity.Player.SessionEnd
            };

            var virtualMachines = await Alloy.GetEventVirtualMachinesAsync(evt.Id);

            foreach (var virtualMachine in virtualMachines)
            {
                state.Vms.Add(new VmState
                {
                    Id = virtualMachine.Url,
                    Name = virtualMachine.Name,
                    IsVisible = true,
                    IsRunning = true,
                    IsolationId = virtualMachine.Id
                });
            }

            var questions = await Alloy.GetEventQuestionsAsync(evt.Id);
            entity.Points = (int)questions.Sum(x => x.Weight);

            state.Challenge = new ChallengeView()
            {
                Attempts = 0,
                MaxAttempts = 10,
                MaxPoints = entity.Points,
                Score = 0,
                SectionCount = 0,
                SectionIndex = 0,
                SectionScore = 0,
                SectionText = "Section 1",
                Text = "Answer the questions",
                Questions = new List<TopoMojo.Api.Client.QuestionView>()
            };

            foreach (var question in questions)
            {
                state.Challenge.Questions.Add(new TopoMojo.Api.Client.QuestionView
                {
                    Text = question.Text,
                    Weight = question.Weight
                });
            }

            return state;
        }

        public async Task<GameState> PreviewGamespace(string externalId)
        {
            var state = new GameState();
            var eventTemplate = await Alloy.GetEventTemplateAsync(new Guid(externalId));
            state.Markdown = eventTemplate.Description;
            return state;
        }

        public async Task<GameState> GradeChallenge(string challengeId, SectionSubmission model)
        {
            // Ensure each challenge can only have one attempt graded at a time
            using (await LockService.GetChallengeLock(challengeId).LockAsync())
            {
                var challengeEntity = await Store.Load(challengeId);
                var challenge = Mapper.Map<Challenge>(challengeEntity);

                if (challenge.State.Challenge.Attempts >= challenge.State.Challenge.MaxAttempts)
                {
                    throw new ActionForbidden();
                }

                DateTimeOffset ts = DateTimeOffset.UtcNow;

                var eventId = challenge.State.Players.FirstOrDefault().GamespaceId;

                var questionViews = await Alloy.GradeEventAsync(new Guid(eventId), model.Questions.Select(x => x.Answer));
                var grade = questionViews.Where(x => x.IsCorrect && x.IsGraded).Sum(x => x.Weight);

                var state = challenge.State;

                if (grade > state.Challenge.Score)
                    state.Challenge.LastScoreTime = ts;

                state.Challenge.Score = grade;
                state.Challenge.Attempts++;
                state.Id = challengeId;

                if (grade == state.Challenge.MaxPoints ||
                    challenge.State.Challenge.Attempts >= challenge.State.Challenge.MaxAttempts)
                {
                    state.IsActive = false;
                    state.EndTime = DateTimeOffset.UtcNow;

                    await Alloy.EndEventAsync(new Guid(eventId));
                }

                state.Challenge.Questions.Clear();

                foreach (var questionView in questionViews)
                {
                    state.Challenge.Questions.Add(new TopoMojo.Api.Client.QuestionView
                    {
                        Answer = questionView.Answer,
                        IsCorrect = questionView.IsCorrect,
                        IsGraded = questionView.IsGraded,
                        Text = questionView.Text,
                        Weight = questionView.Weight
                    });
                }

                Mapper.Map(state, challengeEntity);
                await Store.Update(challengeEntity);

                return state;
            }
        }

        public async Task<ExternalSpec[]> ListSpecs()
        {
            if (!CrucibleOptions.Enabled)
                return Array.Empty<ExternalSpec>();

            var eventTemplates = await Alloy.GetEventTemplatesAsync();
            return Mapper.Map<ExternalSpec[]>(eventTemplates);
        }

        public async Task CompleteGamespace(Data.Challenge entity)
        {
            var challenge = Mapper.Map<Challenge>(entity);
            var eventId = challenge.State.Players.FirstOrDefault().GamespaceId;
            await Alloy.EndEventAsync(new Guid(eventId));
        }
    }
}
