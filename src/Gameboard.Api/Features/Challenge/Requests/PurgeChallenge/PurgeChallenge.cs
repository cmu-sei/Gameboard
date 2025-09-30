// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Challenges;

public record PurgeChallengeCommand(string ChallengeId) : IRequest;

internal sealed class PurgeChallengeHandler(ChallengeService challenges, IValidatorService validator) : IRequestHandler<PurgeChallengeCommand>
{
    private readonly ChallengeService _challenges = challenges;
    private readonly IValidatorService _validator = validator;

    public async Task Handle(PurgeChallengeCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequireOneOf(PermissionKey.Teams_EditSession))
            .AddEntityExistsValidator<Data.Challenge>(request.ChallengeId)
            .Validate(cancellationToken);

        await _challenges.ArchiveChallenge(request.ChallengeId, cancellationToken);
    }
}
