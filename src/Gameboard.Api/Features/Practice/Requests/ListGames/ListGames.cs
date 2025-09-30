// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record ListGamesQuery() : IRequest<ListGamesResponse>;

internal sealed class ListGamesHandler
(
    IPracticeService practiceService
) : IRequestHandler<ListGamesQuery, ListGamesResponse>
{
    public async Task<ListGamesResponse> Handle(ListGamesQuery request, CancellationToken cancellationToken)
    {
        // NOTE: this is an open endpoint, anon ok
        var queryBase = await practiceService.GetPracticeChallengesQueryBase(includeHiddenChallengesIfHasPermission: false);
        var games = await queryBase
            .Select(c => new
            {
                c.Id,
                c.GameId,
                c.Game.Name
            })
            .GroupBy(c => new { Id = c.GameId, c.Name })
            .ToDictionaryAsync(kv => kv.Key, kv => kv.Count(), cancellationToken);

        return new ListGamesResponse
        (
            [
                .. games.Select(kv => new ListGamesResponseGame
                {
                    Id = kv.Key.Id,
                    Name = kv.Key.Name,
                    ChallengeCount = kv.Value
                })
                .OrderBy(g => g.Name)
            ]
        );
    }
}
