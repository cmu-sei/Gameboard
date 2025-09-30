// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Consoles.Validators;

public interface ICanAccessConsoleValidator : IGameboardValidator
{
    string ChallengeId { get; set; }
}

internal sealed class CanAccessConsoleValidator
(
    IActingUserService actingUserService,
    ChallengeService challengesService,
    ILogger<CanAccessConsoleValidator> logger,
    IUserRolePermissionsService permissionsService
) : ICanAccessConsoleValidator
{
    public string ChallengeId { get; set; }

    public Func<RequestValidationContext, Task> GetValidationTask(CancellationToken cancellationToken)
    {
        return async ctx =>
        {
            var actingUser = actingUserService.Get();

            var isTeamMember = await challengesService.UserIsPlayingChallenge(ChallengeId, actingUser.Id);
            logger.LogInformation($"Console access attempt ({ChallengeId} / {actingUser.Id}): User {actingUser.Id}, roles {actingUser.Role}, on team = {isTeamMember} .");

            if (!isTeamMember)
            {
                var hasObserve = await permissionsService.Can(PermissionKey.Teams_Observe);

                if (!hasObserve)
                {
                    var challenge = await challengesService.Get(ChallengeId);
                    ctx.AddValidationException(new UserIsntOnTeam(actingUser.Id, challenge.TeamId));
                    return;
                }
            }

            logger.LogInformation("Console access attempt ({summary}): Allowed.", $"{ChallengeId} / {actingUser.Id}");
        };
    }
}
