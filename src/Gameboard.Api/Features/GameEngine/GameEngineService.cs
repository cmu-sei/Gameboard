// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;
using Alloy.Api.Client;

namespace Gameboard.Api.Services
{
    public class GameEngineService : _Service
    {
        ITopoMojoApiClient Mojo { get; }
        CrucibleService Crucible { get; }
        IAlloyApiClient Alloy { get; }

        private IMemoryCache _localcache;
        private ConsoleActorMap _actorMap;

        public GameEngineService(
            ILogger<GameEngineService> logger,
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
            Alloy = alloy;
            Crucible = crucible;
        }

        public async Task<GameState> RegisterGamespace(Data.ChallengeSpec spec, NewChallenge model, Data.Game game,
        Data.Player player, Data.Challenge entity, int playerCount, string graderKey, string graderUrl)
        {
            GameState state = null;

            switch (spec.GameEngineType)
            {
                case GameEngineType.TopoMojo:

                    state = await Mojo.RegisterGamespaceAsync(new GamespaceRegistration
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
                    break;

                case GameEngineType.Crucible:
                    state = await Crucible.RegisterGamespace(spec, game, player, entity);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return state;
        }

        public async Task<GameState> GetPreview(Data.ChallengeSpec spec)
        {
            GameState state = null;

            switch (spec.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    state = await Mojo.PreviewGamespaceAsync(spec.ExternalId);
                    break;

                case GameEngineType.Crucible:
                    state = await Crucible.PreviewGamespace(spec.ExternalId);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return state;
        }

        public async Task<GameState> GradeChallenge(Data.Challenge entity, SectionSubmission model)
        {
            Task<GameState> gradingTask;

            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    gradingTask = Mojo.GradeChallengeAsync(model);
                    break;

                case GameEngineType.Crucible:
                    gradingTask = Crucible.GradeChallenge(entity.Id, model);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return await gradingTask;
        }

        public async Task<GameState> RegradeChallenge(Data.Challenge entity)
        {
            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    return await Mojo.RegradeChallengeAsync(entity.Id);

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

        public async Task<SectionSubmission[]> AuditChallenge(Data.Challenge entity)
        {
            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    var result = await Mojo.AuditChallengeAsync(entity.Id);
                    return result.ToArray();

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

        public async Task<GameState> LoadGamespace(Data.Challenge entity)
        {
            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    return await Mojo.LoadGamespaceAsync(entity.Id);

                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<GameState> StartGamespace(Data.Challenge entity)
        {
            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    return await Mojo.StartGamespaceAsync(entity.Id);

                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<GameState> StopGamespace(Data.Challenge entity)
        {
            switch (entity.GameEngineType)
            {
                case GameEngineType.TopoMojo:
                    return await Mojo.StopGamespaceAsync(entity.Id);

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
}
