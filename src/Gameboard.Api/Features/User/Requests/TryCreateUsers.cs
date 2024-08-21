using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public sealed record TryCreateUsersCommand(TryCreateUsersRequest Request) : IRequest<TryCreateUsersResponse>;

internal sealed class TryCreateUsersHandler : IRequestHandler<TryCreateUsersCommand, TryCreateUsersResponse>
{
    private readonly IActingUserService _actingUserService;
    private readonly EntityExistsValidator<TryCreateUsersCommand, Data.Game> _gameExists;
    private readonly PlayerService _playerService;
    private readonly EntityExistsValidator<TryCreateUsersCommand, Data.Sponsor> _sponsorExists;
    private readonly IStore _store;
    private readonly UserService _userService;
    private readonly IValidatorService<TryCreateUsersCommand> _validator;

    public TryCreateUsersHandler
    (
        IActingUserService actingUserService,
        EntityExistsValidator<TryCreateUsersCommand, Data.Game> gameExists,
        PlayerService playerService,
        EntityExistsValidator<TryCreateUsersCommand, Data.Sponsor> sponsorExists,
        IStore store,
        UserService userService,
        IValidatorService<TryCreateUsersCommand> validator
    )
    {
        _actingUserService = actingUserService;
        _gameExists = gameExists;
        _playerService = playerService;
        _sponsorExists = sponsorExists;
        _store = store;
        _userService = userService;
        _validator = validator;
    }

    public async Task<TryCreateUsersResponse> Handle(TryCreateUsersCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        _validator.ConfigureAuthorization(config => config.RequirePermissions(PermissionKey.Users_Create));

        // optionally throw if the caller doesn't want to ignore the fact that some users exist already
        if (!request.Request.AllowSubsetCreation)
        {
            _validator.AddValidator(async (req, ctx) =>
        {
            var userIds = req.Request.UserIds.ToArray();
            var existingUserIds = await _store
                .WithNoTracking<Data.User>()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToArrayAsync(cancellationToken);

            if (existingUserIds.Length != 0)
                ctx.AddValidationException(new CantCreateExistingUsers(existingUserIds));
        });
        }

        if (request.Request.EnrollInGameId.IsNotEmpty())
            _validator.AddValidator(_gameExists.UseProperty(r => r.Request.EnrollInGameId));

        if (request.Request.SponsorId.IsNotEmpty())
            _validator.AddValidator(_sponsorExists.UseProperty(r => r.Request.SponsorId));

        await _validator.Validate(request, cancellationToken);

        // do the business
        var createdUsers = new List<TryCreateUserResult>();
        foreach (var id in request.Request.UserIds.ToArray())
        {
            createdUsers.Add(await _userService.TryCreate(new NewUser
            {
                Id = id,
                SponsorId = request.Request.SponsorId,
                UnsetDefaultSponsorFlag = request.Request.UnsetDefaultSponsorFlag
            }));
        }

        // if requested, enroll them in the game
        if (request.Request.EnrollInGameId.IsNotEmpty())
        {
            var actingUser = _actingUserService.Get();

            // - query to determine if anyone is already enrolled
            var createdUserIds = createdUsers.Select(u => u.User.Id).ToArray();
            var enrolledUserIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => createdUserIds.Contains(p.UserId))
                .Where(p => p.Mode == p.Game.PlayerMode)
                .WhereDateIsNotEmpty(p => p.SessionBegin)
                .Select(p => p.UserId)
                .ToArrayAsync(cancellationToken);

            foreach (var createdUser in createdUsers)
            {
                if (enrolledUserIds.Contains(createdUser.User.Id))
                    continue;

                await _playerService.Enroll(new NewPlayer
                {
                    GameId = request.Request.EnrollInGameId,
                    UserId = createdUser.User.Id
                }, actingUser, cancellationToken);
            }
        }

        // as a convenience, we include the name of the sponsor assigned in the response. 
        // this is currently the same for all created users, but you know. you never know.
        var sponsorIds = createdUsers.Select(u => u.User.SponsorId).Distinct().ToArray();

        // for reasons that currently melt my brain, trying to group this query by Id and Name
        // to project to a dictionary on the server side caused insane errors, thanks EF (i'm pretty sure)
        var sponsors = await _store
            .WithNoTracking<Data.Sponsor>()
            .Where(s => sponsorIds.Contains(s.Id))
            .Select(s => new SimpleEntity { Id = s.Id, Name = s.Name })
            .ToArrayAsync(cancellationToken);
        var sponsorNames = sponsors
            .GroupBy(s => new { s.Id, s.Name })
            .ToDictionary(s => s.Key.Id, s => s.Key.Name);

        return new TryCreateUsersResponse
        {
            Users = createdUsers.Select(u => new TryCreateUsersResponseUser
            {
                Id = u.User.Id,
                Name = u.User.ApprovedName,
                IsNewUser = u.IsNewUser,
                EnrolledInGameId = request.Request.EnrollInGameId,
                Sponsor = new SimpleEntity { Id = u.User.SponsorId, Name = sponsorNames[u.User.SponsorId] }
            })
        };
    }
}
