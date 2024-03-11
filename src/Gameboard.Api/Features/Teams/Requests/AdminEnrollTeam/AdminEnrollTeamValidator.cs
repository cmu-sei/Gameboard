using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

internal class AdminEnrollTeamValidator : IGameboardRequestValidator<AdminEnrollTeamRequest>
{
    private readonly EntityExistsValidator<AdminEnrollTeamRequest, Data.Game> _gameExists;
    private readonly ISponsorService _sponsorService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<AdminEnrollTeamRequest> _validator;

    public AdminEnrollTeamValidator
    (
        EntityExistsValidator<AdminEnrollTeamRequest, Data.Game> gameExists,
        ISponsorService sponsorService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<AdminEnrollTeamRequest> validator
    )
    {
        _gameExists = gameExists;
        _sponsorService = sponsorService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Validate(AdminEnrollTeamRequest request, CancellationToken cancellationToken)
    {
        // only registrars/admins
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Registrar)
            .Authorize();

        if (request.PlayerMode == PlayerMode.Practice)
            throw new NotImplementedException($"This feature only allows registration for competitive games.");

        // game must exist and have legal player counts
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Select(g => new
            {
                g.Id,
                g.MinTeamSize,
                g.MaxTeamSize
            })
            .SingleOrDefaultAsync(g => g.Id == request.GameId, cancellationToken);

        _validator.AddValidator(async (req, ctx) =>
        {
            var gameInfo = await _store
                .WithNoTracking<Data.Game>()
                .Where(g => g.Id == req.GameId)
                .Select(g => new
                {
                    g.Id,
                    g.PlayerMode,
                    g.MinTeamSize,
                    g.MaxTeamSize
                })
                .SingleOrDefaultAsync();

            if (gameInfo is null)
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.GameId));

            if (gameInfo.MaxTeamSize < req.UserIds.Count() || gameInfo.MinTeamSize > req.UserIds.Count())
                ctx.AddValidationException(new CantJoinTeamBecausePlayerCount(req.GameId, req.UserIds.Count(), 0, gameInfo.MinTeamSize, gameInfo.MaxTeamSize));

            var allUserIds = new List<string>(req.UserIds);
            if (req.CaptainUserId.IsNotEmpty() && !allUserIds.Any(uId => uId == request.CaptainUserId))
                allUserIds.Add(req.CaptainUserId);

            var knownUsers = await _store
                .WithNoTracking<Data.User>()
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.SponsorId
                })
                .ToArrayAsync();

            var unknownUserIds = allUserIds
                .Where(uId => !knownUsers.Any(u => u.Id == uId));

            foreach (var unknownId in unknownUserIds)
                ctx.AddValidationException(new ResourceNotFound<Data.User>(unknownId));

            // make sure no one is already enrolled if competitive mode
            if (req.PlayerMode == PlayerMode.Competition)
            {
                var knownUserIds = knownUsers.Select(u => u.Id).ToArray();
                var userIdsWithCompetitiveRecord = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.GameId == req.GameId)
                    .Where(p => knownUserIds.Contains(p.UserId))
                    .Where(p => p.Mode == PlayerMode.Competition)
                    .Select(p => p.Id)
                    .ToArrayAsync();

                foreach (var userPlayedPreviously in userIdsWithCompetitiveRecord)
                    ctx.AddValidationException(new AlreadyRegistered(userPlayedPreviously, req.GameId));
            }

            // make sure everyone has a sponsor that isn't the default
            var defaultSponsor = await _sponsorService.GetDefaultSponsor();
            foreach (var user in knownUsers)
            {
                if (user.SponsorId == defaultSponsor.Id)
                    ctx.AddValidationException(new CantEnrollWithDefaultSponsor(user.Id, req.GameId));
            }
        });

        await _validator.Validate(request, cancellationToken);
    }
}
