// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;
using Alloy.Api.Client;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineService : _Service, IGameEngineService
{
    ITopoMojoApiClient Mojo { get; }
    CrucibleService Crucible { get; }
    IAlloyApiClient Alloy { get; }

    private IMemoryCache _localcache;
    private ConsoleActorMap _actorMap;
    private readonly IGameEngineStore _store;

    public GameEngineService(
        ILogger<GameEngineService> logger,
        IGameEngineStore store,
        IMapper mapper,
        CoreOptions options,
        ITopoMojoApiClient mojo,
        IMemoryCache localcache,
        ConsoleActorMap actorMap,
        IAlloyApiClient alloy,
        CrucibleService crucible
    ) : base(logger, mapper, options)
    {
        Mojo = mojo;
        _localcache = localcache;
        _actorMap = actorMap;
        _store = store;
        Alloy = alloy;
        Crucible = crucible;
    }

    public async Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration)
    {
        switch (registration.ChallengeSpec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.RegisterGamespaceAsync(new GamespaceRegistration
                {
                    Players = new RegistrationPlayer[]
                    {
                            new RegistrationPlayer
                            {
                                SubjectId = registration.Player.TeamId,
                                SubjectName = registration.Player.ApprovedName
                            }
                    },
                    ResourceId = registration.ChallengeSpec.ExternalId,
                    Variant = registration.Variant,
                    Points = registration.ChallengeSpec.Points,
                    MaxAttempts = registration.Game.MaxAttempts,
                    StartGamespace = true,
                    ExpirationTime = registration.Player.SessionEnd,
                    GraderKey = registration.GraderKey,
                    GraderUrl = registration.GraderUrl,
                    PlayerCount = registration.PlayerCount
                });

                return Mapper.Map<GameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await Crucible.RegisterGamespace(registration.ChallengeSpec, registration.Game, registration.Player, registration.Challenge);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec)
    {
        switch (spec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.PreviewGamespaceAsync(spec.ExternalId);
                return Mapper.Map<GameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await Crucible.PreviewGamespace(spec.ExternalId);
            default:
                throw new NotImplementedException();
        }
    }

    public Task<IEnumerable<GameEngineGameState>> GetGameState(string teamId)
        => _store.GetGameStatesByTeam(teamId);

    public async Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var gradingResult = await Mojo.GradeChallengeAsync(Mapper.Map<TopoMojo.Api.Client.SectionSubmission>(model));
                return Mapper.Map<GameEngineGameState>(gradingResult);
            case GameEngineType.Crucible:
                return await Crucible.GradeChallenge(entity.Id, model);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<GameEngineGameState>(await Mojo.RegradeChallengeAsync(entity.Id));

            default:
                throw new NotImplementedException();
        }
    }

    public async Task<ConsoleSummary> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer)
    {
        switch (model.Action)
        {
            case ConsoleAction.Ticket:
                {
                    switch (entity.GameEngineType)
                    {
                        case GameEngineType.TopoMojo:
                            return Mapper.Map<ConsoleSummary>(
                            await Mojo.GetVmTicketAsync(model.Id)
                        );

                        default:
                            throw new NotImplementedException();
                    }
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

    public async Task<ExternalSpec[]> ListSpecs(SearchFilter model)
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

            crucibleTask = Crucible.ListSpecs();
            tasks.Add(crucibleTask);

            await Task.WhenAll(tasks);
        }
        catch (Exception) { }

        if (mojoTask != null && mojoTask.IsCompletedSuccessfully)
        {
            resultsList.AddRange(Mapper.Map<ExternalSpec[]>(
                mojoTask.Result
            ));
        }

        if (crucibleTask != null && crucibleTask.IsCompletedSuccessfully)
            resultsList.AddRange(crucibleTask.Result);

        return resultsList.ToArray();
    }

    public async Task<GameEngineGameState> LoadGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<GameEngineGameState>(await Mojo.LoadGamespaceAsync(entity.Id));
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> StartGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<GameEngineGameState>(await Mojo.StartGamespaceAsync(entity.Id));

            default:
                throw new NotImplementedException();
        }
    }

    public async Task<GameEngineGameState> StopGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<GameEngineGameState>(await Mojo.StopGamespaceAsync(entity.Id));

            default:
                throw new NotImplementedException();
        }
    }

    public async Task DeleteGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                await Mojo.DeleteGamespaceAsync(entity.Id);
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
                await Crucible.CompleteGamespace(entity);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mojo.UpdateGamespaceAsync(new ChangedGamespace
                {
                    Id = entity.Id,
                    ExpirationTime = sessionEnd
                });

            default:
                throw new NotImplementedException();
        }
    }
}
