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

    public async Task<IGameEngineGameState> RegisterGamespace(Data.ChallengeSpec spec, NewChallenge model, Data.Game game,
    Data.Player player, Data.Challenge entity, int playerCount, string graderKey, string graderUrl)
    {
        switch (spec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.RegisterGamespaceAsync(new GamespaceRegistration
                {
                    Players = new RegistrationPlayer[]
                    {
                            new RegistrationPlayer
                            {
                                SubjectId = player.TeamId,
                                SubjectName = player.Name
                            }
                    },
                    ResourceId = entity.ExternalId,
                    Variant = model.Variant,
                    Points = spec.Points,
                    MaxAttempts = game.MaxAttempts,
                    StartGamespace = true,
                    ExpirationTime = entity.Player.SessionEnd,
                    GraderKey = graderKey,
                    GraderUrl = graderUrl,
                    PlayerCount = playerCount
                });

                return Mapper.Map<IGameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await Crucible.RegisterGamespace(spec, game, player, entity);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<IGameEngineGameState> GetPreview(Data.ChallengeSpec spec)
    {
        switch (spec.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var topoState = await Mojo.PreviewGamespaceAsync(spec.ExternalId);
                return Mapper.Map<IGameEngineGameState>(topoState);
            case GameEngineType.Crucible:
                return await Crucible.PreviewGamespace(spec.ExternalId);
            default:
                throw new NotImplementedException();
        }
    }

    public Task<IGameEngineGameState> GetGameState(string teamId)
        => _store.GetGameStateByTeam(teamId);

    public async Task<IGameEngineGameState> GradeChallenge(Data.Challenge entity, IGameEngineSectionSubmission model)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var gradingResult = await Mojo.GradeChallengeAsync(Mapper.Map<TopoMojo.Api.Client.SectionSubmission>(model));
                return Mapper.Map<IGameEngineGameState>(gradingResult);
            case GameEngineType.Crucible:
                return await Crucible.GradeChallenge(entity.Id, model);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<IGameEngineGameState> RegradeChallenge(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<IGameEngineGameState>(await Mojo.RegradeChallengeAsync(entity.Id));

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

    public async Task<IEnumerable<IGameEngineSectionSubmission>> AuditChallenge(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                var submissions = await Mojo.AuditChallengeAsync(entity.Id);
                return Mapper.Map<IEnumerable<IGameEngineSectionSubmission>>(submissions);
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
                mojoTask = Mojo.ListWorkspacesAsync(
                "", "", null, null,
                model.Term, model.Skip, model.Take, model.Sort,
                model.Filter);

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

    public async Task<IGameEngineGameState> LoadGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<IGameEngineGameState>(await Mojo.LoadGamespaceAsync(entity.Id));
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<IGameEngineGameState> StartGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<IGameEngineGameState>(await Mojo.StartGamespaceAsync(entity.Id));

            default:
                throw new NotImplementedException();
        }
    }

    public async Task<IGameEngineGameState> StopGamespace(Data.Challenge entity)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                return Mapper.Map<IGameEngineGameState>(await Mojo.StopGamespaceAsync(entity.Id));

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

    public async Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd)
    {
        switch (entity.GameEngineType)
        {
            case GameEngineType.TopoMojo:
                await Mojo.UpdateGamespaceAsync(new ChangedGamespace
                {
                    Id = entity.Id,
                    ExpirationTime = sessionEnd
                });
                break;

            default:
                throw new NotImplementedException();
        }
    }
}
