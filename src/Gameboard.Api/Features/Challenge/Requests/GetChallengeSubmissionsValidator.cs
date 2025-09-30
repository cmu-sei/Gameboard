// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

internal class GetChallengeSubmissionsValidator(
    IActingUserService actingUserService,
    IUserRolePermissionsService permissionsService,
    IStore store,
    IValidatorService<GetChallengeSubmissionsQuery> validatorService
    ) : IGameboardRequestValidator<GetChallengeSubmissionsQuery>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;
    private readonly IValidatorService<GetChallengeSubmissionsQuery> _validatorService = validatorService;

    public async Task Validate(GetChallengeSubmissionsQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(async (req, ctx) =>
        {
            // check if challenge exists
            var challenge = await _store
                .WithNoTracking<Data.Challenge>()
                .SingleOrDefaultAsync(c => c.Id == req.ChallengeId, cancellationToken);

            if (challenge is null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Challenge>(req.ChallengeId));
                return;
            }

            // check that the acting user is a player on the challenge team or an admin
            if (!await _permissionsService.Can(PermissionKey.Teams_Observe))
            {
                var actingUser = _actingUserService.Get();

                var isUserOnTeam = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.UserId == actingUser.Id)
                    .Where(p => p.TeamId == challenge.TeamId)
                    .AnyAsync(cancellationToken);

                if (!isUserOnTeam)
                {
                    ctx.AddValidationException(new UserIsntOnTeam(actingUser.Id, challenge.TeamId));
                    return;
                }
            }
        });

        await _validatorService.Validate(request, cancellationToken);
    }
}
