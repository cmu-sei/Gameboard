// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Challenges;

public record SyncChallengeCommand(string ChallengeId) : IRequest<GameEngineGameState>;

internal sealed class SyncChallengeHandler
(
    IChallengeSyncService challengeSyncService,
    IValidatorService validator
) : IRequestHandler<SyncChallengeCommand, GameEngineGameState>
{
    public async Task<GameEngineGameState> Handle(SyncChallengeCommand request, CancellationToken cancellationToken)
    {
        await validator
            .Auth(c => c.Require(PermissionKey.Admin_View))
            .AddEntityExistsValidator<Data.Challenge>(request.ChallengeId)
            .Validate(cancellationToken);

        return await challengeSyncService.Sync(request.ChallengeId, cancellationToken);
    }
}
