// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TopoMojo.Api.Client;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Features.Consoles;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineService
{
    Task<IEnumerable<GameEngineSectionSubmission>> AuditChallenge(Data.Challenge entity);
    Task CompleteGamespace(Data.Challenge entity);
    Task CompleteGamespace(string id, GameEngineType gameEngineType);
    Task DeleteGamespace(Data.Challenge entity);
    Task DeleteGamespace(string id, GameEngineType gameEngineType);
    Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd);
    Task ExtendSession(string challengeId, DateTimeOffset sessionEnd, GameEngineType gameEngineType);
    Task<GameEngineChallengeProgressView> GetChallengeProgress(string challengeId, GameEngineType gameEngineType, CancellationToken cancellationToken);
    Task<GameEngineGameState> GetChallengeState(GameEngineType gameEngineType, string stateJson);
    Task<ConsoleState> GetConsole(GameEngineType gameEngine, ConsoleId console, CancellationToken cancellationToken);
    Task<ConsoleState> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer);
    Task<ConsoleState[]> GetConsoles(GameEngineType gameEngine, ConsoleId[] consoleIds, CancellationToken cancellationToken);
    Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec);
    IEnumerable<GameEngineGamespaceVm> GetVmsFromState(GameEngineGameState state);
    Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model);
    Task<ExternalSpec[]> ListGameEngineSpecs(SearchFilter model);
    Task<GameEngineGameState> LoadGamespace(Data.Challenge entity);
    Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration);
    Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity);
    Task<GameEngineGameState> StartGamespace(GameEngineGamespaceStartRequest request);
    Task<GameEngineGameState> StopGamespace(Data.Challenge entity);
}

public class GameEngineService
(
    IJsonService jsonService,
    ILogger<GameEngineService> logger,
    IMapper mapper,
    CoreOptions options,
    IHttpClientFactory httpClientFactory,
    ITopoMojoApiClient mojo,
    ICrucibleService crucible,
    IVmUrlResolver vmUrlResolver
) : _Service(logger, mapper, options), IGameEngineService
{
    ITopoMojoApiClient Mojo { get; } = mojo;

    private readonly ICrucibleService _crucible = crucible;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IJsonService _jsonService = jsonService;
    private readonly IVmUrlResolver _vmUrlResolver = vmUrlResolver;

    public async Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration)
    {
        switch (registration.ChallengeSpec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.RegisterGamespaceAsync(new GamespaceRegistration
                {
                    Players =
                    [
                        new()
                        {
                            SubjectId = registration.Player.TeamId,
                            SubjectName = registration.Player.ApprovedName
                        }
                    ],
                    ResourceId = registration.ChallengeSpec.ExternalId,
                    Variant = registration.Variant,
                    Points = registration.ChallengeSpec.Points,
                    MaxAttempts = registration.AttemptLimit,
                    StartGamespace = registration.StartGamespace,
                    ExpirationTime = registration.Player.SessionEnd,
                    GraderKey = registration.GraderKey,
                    GraderUrl = registration.GraderUrl,
                    PlayerCount = registration.PlayerCount
                });

                return Mapper.Map<GameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await _crucible.RegisterGamespace(registration.ChallengeSpec, registration.Game, registration.Player, registration.Challenge, registration.AttemptLimit);
            default:
                throw new NotImplementedException();
        }
    }

    public Task<GameEngineGameState> GetChallengeState(GameEngineType gameEngineType, string stateJson)
    {
        return gameEngineType switch
        {
            GameEngineType.TopoMojo => Task.FromResult(Mapper.Map<GameEngineGameState>(_jsonService.Deserialize<GameState>(stateJson))),
            _ => throw new NotImplementedException(),
        };
    }

    public async Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec)
    {
        switch (spec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.PreviewGamespaceAsync(spec.ExternalId);
                return Mapper.Map<GameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await _crucible.PreviewGamespace(spec.ExternalId);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                try
                {
                    var gradingResult = await Mojo.GradeChallengeAsync(Mapper.Map<TopoMojo.Api.Client.SectionSubmission>(model));
                    return Mapper.Map<GameEngineGameState>(gradingResult);
                }
                catch (TopoMojo.Api.Client.ApiException ex)
                {
                    if (ex.Message.Contains("GamespaceIsExpired"))
                        throw new SubmissionIsForExpiredGamespace(entity.Id, ex);

                    throw new GradingFailed(entity.Id, ex);
                }
            case GameEngineType.Crucible:
                return await _crucible.GradeChallenge(entity.Id, model);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity)
    {
        return entity.GameEngineType switch
        {
            GameEngineType.TopoMojo => Mapper.Map<GameEngineGameState>(await Mojo.RegradeChallengeAsync(entity.Id)),
            _ => throw new NotImplementedException(),
        };
    }

    public async Task<ConsoleState> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer)
    {
        switch (model.Action)
        {
            case ConsoleAction.Ticket:
                {
                    return entity.GameEngineType switch
                    {
                        GameEngineType.TopoMojo => Mapper.Map<ConsoleState>(await Mojo.GetVmTicketAsync(model.Id)),
                        _ => throw new NotImplementedException(),
                    };
                }
            case ConsoleAction.Reset:
                {
                    switch (entity.GameEngineType)
                    {
                        case GameEngineType.TopoMojo:
                            var vm = await Mojo.ChangeVmAsync(
                                new VmOperation
                                {
                                    Id = model.Id,
                                    Type = VmOperationType.Reset
                                }
                            );

                            return new ConsoleState
                            {
                                Id = new ConsoleId { ChallengeId = model.SessionId, Name = vm.Name },
                                AccessTicket = string.Empty,
                                IsRunning = vm.State == VmPowerState.Running,
                                Url = vm.Path
                            };

                        default:
                            throw new NotImplementedException();
                    }
                }
        }

        return null;
    }

    public async Task<ConsoleState> GetConsole(GameEngineType gameEngine, ConsoleId consoleId, CancellationToken cancellationToken)
    {
        var consoles = await GetConsoles(gameEngine, [consoleId], cancellationToken);

        if (consoles.Length != 1)
        {
            throw new GameEngineException($"Couldn't resolve console {consoleId.ToString()} on game engine {gameEngine}");
        }

        return consoles[0];
    }

    public async Task<ConsoleState[]> GetConsoles(GameEngineType gameEngine, ConsoleId[] consoleIds, CancellationToken cancellationToken)
    {
        if (gameEngine != GameEngineType.TopoMojo)
        {
            throw new NotImplementedException("Non-Topo game engines are currently unsupported.");
        }

        var tasks = consoleIds.Select(id => Mojo.GetVmTicketAsync(id.ToString()));
        var results = await Task.WhenAll(tasks);
        var runningVms = results.Where(r => r.IsRunning).ToArray();

        return [.. runningVms.Select(vm => new ConsoleState
        {
            Id = new ConsoleId() { ChallengeId = vm.IsolationId, Name = vm.Name },
            AccessTicket = vm.Ticket,
            IsRunning = vm.IsRunning,
            Url = vm.Url
        })];
    }

    public IEnumerable<GameEngineGamespaceVm> GetVmsFromState(GameEngineGameState state)
        => [.. state.Vms.Select(vm => new GameEngineGamespaceVm
        {
            Id = vm.Id,
            Name = vm.Name,
            Url = _vmUrlResolver.ResolveUrl(vm)
        })];

    public async Task<IEnumerable<GameEngineSectionSubmission>> AuditChallenge(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var submissions = await Mojo.AuditChallengeAsync(entity.Id);
                return Mapper.Map<IEnumerable<GameEngineSectionSubmission>>(submissions);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<ExternalSpec[]> ListGameEngineSpecs(SearchFilter model)
    {
        var resultsList = new List<ExternalSpec>();

        var tasks = new List<Task>();
        Task<ICollection<WorkspaceSummary>> mojoTask = null;
        Task<ExternalSpec[]> crucibleTask = null;

        try
        {
            if (Options.MojoEnabled)
            {
                mojoTask = Mojo.ListWorkspacesAsync
                (
                    "",             // audience
                    "",             // scope
                    1,              // doc
                    model.Term,
                    model.Skip,
                    model.Take,
                    model.Sort,
                    model.Filter
                );

                tasks.Add(mojoTask);
            }

            if (_crucible.IsEnabled())
            {
                crucibleTask = _crucible.ListSpecs();
                tasks.Add(crucibleTask);
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Couldn't reach the game engine: {exceptionSummary}", $"{ex.GetType().Name}: {ex.Message}");
            throw;
        }

        if (mojoTask != null && mojoTask.IsCompletedSuccessfully)
        {
            resultsList.AddRange(Mapper.Map<ExternalSpec[]>(mojoTask.Result));
        }

        if (crucibleTask != null && crucibleTask.IsCompletedSuccessfully)
            resultsList.AddRange(crucibleTask.Result);

        return resultsList.ToArray();
    }

    public async Task<GameEngineGameState> LoadGamespace(Data.Challenge entity)
    {
        return entity.GameEngineType switch
        {
            GameEngineType.TopoMojo => Mapper.Map<GameEngineGameState>(await Mojo.LoadGamespaceAsync(entity.Id)),
            _ => throw new NotImplementedException(),
        };
    }

    public async Task<GameEngineGameState> StartGamespace(GameEngineGamespaceStartRequest request)
    {
        if (request.GameEngineType == GameEngineType.TopoMojo)
        {
            try
            {
                var state = await Mojo.StartGamespaceAsync(request.ChallengeId);
                return Mapper.Map<GameEngineGameState>(state);
            }
            catch (TopoMojo.Api.Client.ApiException ex)
            {
                throw new GamespaceStartFailure(request.ChallengeId, GameEngineType.TopoMojo, ex);
            }

        }

        // we don't have an alloy implementation yet
        throw new NotImplementedException();
    }

    public async Task<GameEngineGameState> StopGamespace(Data.Challenge entity)
    {
        return entity.GameEngineType switch
        {
            GameEngineType.TopoMojo => Mapper.Map<GameEngineGameState>(await Mojo.StopGamespaceAsync(entity.Id)),
            _ => throw new NotImplementedException(),
        };
    }

    public Task DeleteGamespace(Data.Challenge entity)
        => DeleteGamespace(entity.Id, entity.GameEngineType);

    public async Task DeleteGamespace(string id, GameEngineType gameEngineType)
    {
        switch (gameEngineType)
        {
            case GameEngineType.TopoMojo:
                await Mojo.DeleteGamespaceAsync(id);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public async Task CompleteGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                await Mojo.CompleteGamespaceAsync(entity.Id);
                break;

            case GameEngineType.Crucible:
                await _crucible.CompleteGamespace(entity);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public async Task CompleteGamespace(string id, GameEngineType gameEngineType)
    {
        switch (gameEngineType)
        {
            case GameEngineType.TopoMojo:
                await Mojo.CompleteGamespaceAsync(id);
                break;
            default:
                throw new NotImplementedException("Crucible's engine doesn't implement this signature. To complete a Crucible gamespace, use the overload that accepts a Challenge argument.");
        }
    }

    public Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd)
        => ExtendSession(entity.Id, sessionEnd, entity.GameEngineType);

    public Task ExtendSession(string challengeId, DateTimeOffset sessionEnd, GameEngineType gameEngineType)
    {
        return gameEngineType switch
        {
            GameEngineType.TopoMojo => Mojo.UpdateGamespaceAsync(new ChangedGamespace
            {
                Id = challengeId,
                ExpirationTime = sessionEnd
            }),
            _ => throw new NotImplementedException(),
        };
    }

    public async Task<GameEngineChallengeProgressView> GetChallengeProgress(string challengeId, GameEngineType gameEngineType, CancellationToken cancellationToken)
    {
        switch (gameEngineType)
        {
            case GameEngineType.TopoMojo:
                // we have to do some calculations and stuff, particularly around weighting and scoring, to translate these
                // to GB types
                var topoProgress = await Mojo.LoadGamespaceChallengeProgressAsync(challengeId, cancellationToken);
                var progress = new GameEngineChallengeProgressView
                {
                    Id = topoProgress.Id,
                    Attempts = topoProgress.Attempts,
                    ExpiresAtTimestamp = topoProgress.ExpiresAtTimestamp,
                    MaxAttempts = topoProgress.MaxAttempts,
                    MaxPoints = topoProgress.MaxPoints,
                    LastScoreTime = topoProgress.LastScoreTime,
                    NextSectionPreReqThisSection = EngineWeightToScore(topoProgress.NextSectionPreReqThisSection, topoProgress.MaxPoints),
                    NextSectionPreReqTotal = EngineWeightToScore(topoProgress.NextSectionPreReqTotal, topoProgress.MaxPoints),
                    Score = topoProgress.Score,
                    Variant = new GameEngineVariantView
                    {
                        Sections = topoProgress.Variant.Sections.Select(s =>
                        {
                            var totalWeight = s.Questions.Sum(q => q.Weight);
                            var scoreMax = EngineWeightToScore(totalWeight, topoProgress.MaxPoints) ?? 0;

                            var section = new GameEngineSectionView
                            {
                                Name = s.Name,
                                PreReqPrevSection = s.PreReqPrevSection,
                                PreReqTotal = s.PreReqTotal,
                                Score = s.Score,
                                ScoreMax = scoreMax,
                                Text = s.Text,
                                TotalWeight = totalWeight,
                                Questions = s.Questions.Select(q =>
                                {
                                    var questionView = Mapper.Map<GameEngineQuestionView>(q);
                                    questionView.ScoreMax = EngineWeightToScore(q.Weight, topoProgress.MaxPoints) ?? 0;
                                    questionView.ScoreCurrent = q.IsCorrect ? questionView.ScoreMax : 0;
                                    return questionView;
                                }).ToArray()
                            };

                            return section;
                        }).ToArray(),
                        Text = topoProgress.Variant.Text,
                        TotalSectionCount = topoProgress.Variant.TotalSectionCount
                    },
                    Text = topoProgress.Text
                };

                return progress;
            default:
                throw new NotImplementedException();
        }
    }

    private double? EngineWeightToScore(double? weight, double maxScore)
    {
        if (weight is null)
            return null;

        return Math.Round(weight.Value * maxScore, 0, MidpointRounding.AwayFromZero);
    }
}
