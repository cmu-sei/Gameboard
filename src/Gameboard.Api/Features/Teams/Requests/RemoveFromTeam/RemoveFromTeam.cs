// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record RemoveFromTeamCommand(string PlayerId) : IRequest<RemoveFromTeamResponse>;

internal sealed class RemoveFromTeamHandler
(
    IGuidService guids,
    EntityExistsValidator<Data.Player> playerExists,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<RemoveFromTeamCommand, RemoveFromTeamResponse>
{
    private readonly IGuidService _guids = guids;
    private readonly EntityExistsValidator<Data.Player> _playerExists = playerExists;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validatorService;

    public async Task<RemoveFromTeamResponse> Handle(RemoveFromTeamCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Teams_Enroll))
            .AddValidator(_playerExists.UseValue(request.PlayerId))
            .AddValidator(async ctx =>
            {
                var playerData = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.Id == request.PlayerId)
                    .Select(p => new
                    {
                        p.Id,
                        p.ApprovedName,
                        p.SessionBegin,
                        p.Role,
                        p.TeamId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                // if they started the session already, tough nuggets
                if (!playerData.SessionBegin.IsEmpty())
                {
                    ctx.AddValidationException(new SessionAlreadyStarted(request.PlayerId, "This player can't be removed from the team."));
                }

                // you can't remove the captain (unenroll them instead)
                if (playerData.Role == PlayerRole.Manager)
                {
                    ctx.AddValidationException(new CantRemoveCaptain(new SimpleEntity { Id = playerData.Id, Name = playerData.ApprovedName }, playerData.TeamId));
                }

                // in theory the last remaining player should be the captain and should get caught by above,
                // but because the schema is weird (shoutout #553), check anyway
                var hasRemainingTeammates = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.TeamId == playerData.TeamId)
                    .Where(p => p.Id != request.PlayerId)
                    .AnyAsync(cancellationToken);

                if (!hasRemainingTeammates)
                {
                    ctx.AddValidationException(new CantRemoveLastTeamMember(new SimpleEntity { Id = playerData.Id, Name = playerData.ApprovedName }, playerData.TeamId));
                }
            })
            .Validate(cancellationToken);

        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == request.PlayerId)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.TeamId, _guids.Generate()), cancellationToken);

        return await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == request.PlayerId)
            .Select(p => new RemoveFromTeamResponse
            {
                Player = new SimpleEntity { Id = p.Id, Name = p.ApprovedName },
                Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Name },
                TeamId = p.TeamId,
                UserId = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName }
            })
            .SingleAsync(cancellationToken);
    }
}
