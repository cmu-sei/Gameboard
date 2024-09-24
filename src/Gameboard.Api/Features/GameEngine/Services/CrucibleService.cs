// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.Extensions.Logging;
using Alloy.Api.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Services;
using Gameboard.Api.Data;
using System.Threading;

namespace Gameboard.Api.Features.GameEngine;

public interface ICrucibleService
{
    Task CompleteGamespace(Data.Challenge entity);
    Task<GameEngineGameState> GradeChallenge(string challengeId, GameEngineSectionSubmission model);
    bool IsEnabled();
    Task<ExternalSpec[]> ListSpecs();
    Task<GameEngineGameState> PreviewGamespace(string externalId);
    Task<GameEngineGameState> RegisterGamespace(Data.ChallengeSpec spec, Data.Game game, Data.Player player, Data.Challenge entity);
}

public class CrucibleService(
    ILogger<ChallengeService> logger,
    IMapper mapper,
    CoreOptions options,
    IAlloyApiClient alloy,
    CrucibleOptions crucibleOptions,
    ILockService lockService,
    IStore store
    ) : _Service(logger, mapper, options), ICrucibleService
{
    IAlloyApiClient Alloy { get; } = alloy;
    CrucibleOptions CrucibleOptions { get; } = crucibleOptions;
    ILockService LockService { get; } = lockService;

    private readonly IStore _store = store;

    public bool IsEnabled()
        => CrucibleOptions.Enabled;

    public async Task<GameEngineGameState> RegisterGamespace(Data.ChallengeSpec spec, Data.Game game, Data.Player player, Data.Challenge entity)
    {
        var whenCreated = DateTimeOffset.UtcNow;
        var additionalUserIds = new List<Guid>();

        if (game.AllowTeam)
        {
            additionalUserIds = await _store
                .WithNoTracking<Data.Player>()
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

        var state = new GameEngineGameState
        {
            Markdown = spec.Description,
            IsActive = true,
            Players =
            [
                new()
                {
                    GamespaceId = evt.Id.ToString(),
                    SubjectId = player.TeamId,
                    SubjectName = player.ApprovedName
                }
            ],
            Vms = [],
            Name = spec.Name,
            Id = entity.Id,
            WhenCreated = whenCreated,
            StartTime = DateTimeOffset.UtcNow,
            ExpirationTime = entity.Player.SessionEnd
        };

        state.Vms = (await Alloy.GetEventVirtualMachinesAsync(evt.Id)).Select(vm => new GameEngineVmState
        {
            Id = vm.Url,
            Name = vm.Name,
            IsVisible = true,
            IsRunning = true,
            IsolationId = vm.Id
        });

        var questions = await Alloy.GetEventQuestionsAsync(evt.Id);
        entity.Points = (int)questions.Sum(x => x.Weight);

        state.Challenge = new()
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
            Questions = questions.Select(q => new GameEngineQuestionView
            {
                Text = q.Text,
                Weight = q.Weight
            })
        };

        return state;
    }

    public async Task<GameEngineGameState> PreviewGamespace(string externalId)
    {
        var state = new GameEngineGameState();
        var eventTemplate = await Alloy.GetEventTemplateAsync(new Guid(externalId));
        state.Markdown = eventTemplate.Description;
        return state;
    }

    public async Task<GameEngineGameState> GradeChallenge(string challengeId, GameEngineSectionSubmission model)
    {
        // Ensure each challenge can only have one attempt graded at a time
        using (await LockService.GetChallengeLock(challengeId).LockAsync())
        {
            var challengeEntity = await _store.SingleAsync<Data.Challenge>(challengeId, CancellationToken.None);
            var challenge = Mapper.Map<Challenge>(challengeEntity);

            if (challenge.State.Challenge.Attempts >= challenge.State.Challenge.MaxAttempts)
            {
                throw new ActionForbidden();
            }

            var ts = DateTimeOffset.UtcNow;

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

            state.Challenge.Questions = questionViews.Select(qv => new GameEngineQuestionView
            {
                Answer = qv.Answer,
                IsCorrect = qv.IsCorrect,
                IsGraded = qv.IsGraded,
                Text = qv.Text,
                Weight = qv.Weight
            });

            Mapper.Map(state, challengeEntity);
            await _store.SaveUpdate(challengeEntity, CancellationToken.None);

            return Mapper.Map<GameEngineGameState>(state);
        }
    }

    public async Task<ExternalSpec[]> ListSpecs()
    {
        if (!CrucibleOptions.Enabled)
            return [];

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
