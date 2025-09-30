// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.Requests;

public record ListGamesQuery(ListGamesExecutionStatus? ExecutionStatus, PlayerMode? PlayerMode, bool? IsPublished, string SearchTerm) : IRequest<ListGamesResponse>;

internal class ListGamesQueryHandler
(
    INowService now,
    IUserRolePermissionsService permissions,
    IStore store
) : IRequestHandler<ListGamesQuery, ListGamesResponse>
{
    public async Task<ListGamesResponse> Handle(ListGamesQuery request, CancellationToken cancellationToken)
    {
        var nowish = now.Get();
        var baseQuery = store.WithNoTracking<Data.Game>()
            .Include(g => g.Players)
            .AsQueryable();

        // can we see unpublished games?
        var canViewUnpublished = await permissions.Can(PermissionKey.Games_ViewUnpublished);

        // execution filter
        if (request.ExecutionStatus == ListGamesExecutionStatus.Past)
        {
            baseQuery = baseQuery.Where(g => g.GameEnd < nowish);
        }
        else if (request.ExecutionStatus == ListGamesExecutionStatus.Future)
        {
            baseQuery = baseQuery.Where(g => g.GameStart >= nowish);
        }
        else if (request.ExecutionStatus == ListGamesExecutionStatus.Ongoing)
        {
            baseQuery = baseQuery.WhereDateIsEmpty(g => g.GameEnd);
        }
        else if (request.ExecutionStatus == ListGamesExecutionStatus.Live)
        {
            baseQuery = baseQuery.Where(g => g.GameStart <= nowish && g.GameEnd > nowish);
        }

        // mode filter
        if (request.PlayerMode.HasValue)
        {
            baseQuery = baseQuery.Where(g => g.PlayerMode == request.PlayerMode.Value);
        }

        // publication filter
        if (request.IsPublished.HasValue)
        {
            baseQuery = baseQuery.Where(g => g.IsPublished == request.IsPublished && (!g.IsPublished || canViewUnpublished));
        }

        // search term filter
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();

            baseQuery = baseQuery.Where
            (
                g =>
                    g.Name.ToLower().Contains(term) ||
                    g.Season.ToLower().Contains(term) ||
                    g.Track.ToLower().Contains(term) ||
                    g.Division.ToLower().Contains(term) ||
                    g.Competition.ToLower().Contains(term) ||
                    g.Sponsor.ToLower().Contains(term) ||
                    g.Mode.ToLower().Contains(term) ||
                    g.Id.ToLower().StartsWith(term) ||
                    g.CardText1.ToLower().Contains(term) ||
                    g.CardText2.ToLower().Contains(term) ||
                    g.CardText3.ToLower().Contains(term)
            );
        }


        return new ListGamesResponse
        {
            Games = await baseQuery
                .Select(g => new ListGamesResponseGame
                {
                    Id = g.Id,
                    Name = g.Name,
                    Background = g.Background,
                    CardText1 = g.CardText1,
                    CardText2 = g.CardText2,
                    CardText3 = g.CardText3,
                    Logo = g.Logo,
                    SponsorId = g.Sponsor,

                    AllowLateStart = g.AllowLateStart,
                    AllowPreview = g.AllowLateStart,
                    AllowPublicScoreboardAccess = g.AllowPublicScoreboardAccess,
                    AllowReset = g.AllowReset,
                    EngineMode = g.Mode,
                    PlayerMode = g.PlayerMode,
                    GamespaceLimitPerSession = g.GamespaceLimitPerSession,
                    IsPublished = g.IsPublished,
                    MinTeamSize = g.MinTeamSize,
                    MaxTeamSize = g.MaxTeamSize,
                    GameStart = g.GameStart,
                    GameEnd = g.GameEnd,
                    IsFeatured = g.IsFeatured,
                    MaxAttempts = g.MaxAttempts,
                    SessionAvailabilityWarningThreshold = g.SessionAvailabilityWarningThreshold,
                    SessionLimit = g.SessionLimit,
                    SessionMinutes = g.SessionMinutes,

                    Competition = g.Competition,
                    Division = g.Division,
                    Season = g.Season,
                    Track = g.Track,

                    Registration = new ListGameResponseGameRegistration
                    {
                        StartTime = g.RegistrationOpen,
                        EndTime = g.RegistrationClose,
                        RegistrationConstraint = g.RegistrationConstraint,
                        RegistrationType = g.RegistrationType
                    },

                    RegisteredTeamCount = g.Players.Select(p => p.TeamId).Distinct().Count(),
                    RegisteredUserCount = g.Players.Select(p => p.UserId).Distinct().Count()
                })
                .OrderBy(g => g.GameStart <= nowish ? 0 : 1)
                    .ThenBy(g => g.GameStart)
                    .ThenBy(g => g.GameEnd)
                .ToArrayAsync(cancellationToken)
        };
    }
}
