// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Consoles.Validators;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Consoles.Requests;

public record GetConsoleRequest(string ChallengeId, string Name) : IRequest<GetConsoleResponse>;

internal sealed class GetConsoleHandler
(
    IActingUserService actingUserService,
    ICanAccessConsoleValidator canAccessConsoleValidator,
    ChallengeService challengeService,
    IGameEngineService gameEngine,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetConsoleRequest, GetConsoleResponse>
{
    public async Task<GetConsoleResponse> Handle(GetConsoleRequest request, CancellationToken cancellationToken)
    {
        // configure console access validator
        canAccessConsoleValidator.ChallengeId = request.ChallengeId;

        // validate
        await validatorService
            .AddEntityExistsValidator<Data.Challenge>(request.ChallengeId)
            .AddValidator(canAccessConsoleValidator)
            .Validate(cancellationToken);

        // get the console and its state
        var challenge = await store
            .WithNoTracking<Data.Challenge>()
            .Select(c => new { c.Id, c.State, c.GameEngineType, c.EndTime, c.Player.SessionEnd })
            .SingleAsync(c => c.Id == request.ChallengeId, cancellationToken);
        var state = await gameEngine.GetChallengeState(GameEngineType.TopoMojo, challenge.State);

        if (!state.Vms.Any(v => v.Name == request.Name))
        {
            var vmNames = string.Join(", ", state.Vms.Select(vm => vm.Name));
            throw new ResourceNotFound<GameEngineVmState>("n/a", $"VMS for challenge {request.ChallengeId} - searching for {request.Name}, found these names: {vmNames}");
        }

        var console = await gameEngine.GetConsole(challenge.GameEngineType, new ConsoleId() { ChallengeId = request.ChallengeId, Name = request.Name }, cancellationToken) ?? throw new InvalidConsoleAction();
        var isPlayingChallenge = await challengeService.UserIsPlayingChallenge(request.ChallengeId, actingUserService.Get().Id);
        return new GetConsoleResponse
        {
            ConsoleState = console,
            IsViewOnly = !isPlayingChallenge,
            ExpiresAt = challenge.EndTime < challenge.SessionEnd ? challenge.EndTime : challenge.SessionEnd
        };
    }
}
