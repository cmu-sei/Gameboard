// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Alloy.Api.Client;
using TopoMojo.Api.Client;
using Gameboard.Api.Services;
using Gameboard.Api.Common.Services;
using System.Linq;

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
    Task<GameEngineGameState> GetChallengeState(GameEngineType gameEngineType, string stateJson);
    Task<ConsoleSummary> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer);
    Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec);
    IEnumerable<GameEngineGamespaceVm> GetGamespaceVms(GameEngineGameState state);
    Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model);
    Task<ExternalSpec[]> ListGameEngineSpecs(SearchFilter model);
    Task<GameEngineGameState> LoadGamespace(Data.Challenge entity);
    Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration);
    Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity);
    Task<GameEngineGameState> StartGamespace(GameEngineGamespaceStartRequest request);
    Task<GameEngineGameState> StopGamespace(Data.Challenge entity);
}

public class GameEngineService(
    IJsonService jsonService,
    ILogger<GameEngineService> logger,
    IMapper mapper,
    CoreOptions options,
    ITopoMojoApiClient mojo,
    IAlloyApiClient alloy,
    ICrucibleService crucible,
    IVmUrlResolver vmUrlResolver
    ) : _Service(logger, mapper, options), IGameEngineService
{
    ITopoMojoApiClient Mojo { get; } = mojo;
    IAlloyApiClient Alloy { get; } = alloy;

    private readonly ICrucibleService _crucible = crucible;
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
                    MaxAttempts = registration.Game.MaxAttempts,
                    StartGamespace = registration.StartGamespace,
                    ExpirationTime = registration.Player.SessionEnd,
                    GraderKey = registration.GraderKey,
                    GraderUrl = registration.GraderUrl,
                    PlayerCount = registration.PlayerCount
                });

                return Mapper.Map<GameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await _crucible.RegisterGamespace(registration.ChallengeSpec, registration.Game, registration.Player, registration.Challenge);
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

    public async Task<ConsoleSummary> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer)
    {
        switch (model.Action)
        {
            case ConsoleAction.Ticket:
                {
                    return entity.GameEngineType switch
                    {
                        GameEngineType.TopoMojo => Mapper.Map<ConsoleSummary>(await Mojo.GetVmTicketAsync(model.Id)),
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

                            return new ConsoleSummary
                            {
                                Id = vm.Id,
                                Name = vm.Name,
                                SessionId = model.SessionId,
                                IsRunning = vm.State == VmPowerState.Running,
                                IsObserver = observer
                            };

                        default:
                            throw new NotImplementedException();
                    }
                }
        }

        return null;
    }

    public IEnumerable<GameEngineGamespaceVm> GetGamespaceVms(GameEngineGameState state)
    {
        return state.Vms.Select(vm => new GameEngineGamespaceVm
        {
            Id = vm.Id,
            Name = vm.Name,
            Url = _vmUrlResolver.ResolveUrl(vm)
        });
    }

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
                    null,           // wants audience?
                    null,           // wants managed?
                    null,           // wants doc?
                    null,           // wants partial doc?
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
            Logger.LogCritical($"Couldn't reach the game engine: {ex.GetType().Name}: {ex.Message}");
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
}
