using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Admin;

public record GetGameCenterPracticeContextQuery(string GameId, string SearchTerm, GameCenterPracticeSessionStatus? SessionStatus, GameCenterPracticeSort? Sort) : IRequest<GameCenterPracticeContext>;

internal class GetGameCenterPracticeQueryHandler : IRequestHandler<GetGameCenterPracticeContextQuery, GameCenterPracticeContext>
{
    private readonly EntityExistsValidator<GetGameCenterPracticeContextQuery, Data.Game> _gameExists;
    private readonly INowService _now;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetGameCenterPracticeContextQuery> _validatorService;

    public GetGameCenterPracticeQueryHandler
    (
        EntityExistsValidator<GetGameCenterPracticeContextQuery, Data.Game> gameExists,
        INowService now,
        IScoringService scoringService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetGameCenterPracticeContextQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _now = now;
        _scoringService = scoringService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<GameCenterPracticeContext> Handle(GetGameCenterPracticeContextQuery request, CancellationToken cancellationToken)
    {
        // auth/validate
        _userRoleAuthorizer.AllowAllElevatedRoles();
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.GameId));
        await _validatorService.Validate(request, cancellationToken);

        // pull
        var nowish = _now.Get();
        var searchTerm = request.SearchTerm.IsEmpty() ? null : request.SearchTerm.ToLower();

        var users = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.GameId == request.GameId)
            .Where
            (
                c =>
                    searchTerm == null ||
                    c.Id.StartsWith(searchTerm) ||
                    c.Player.UserId.ToLower().StartsWith(searchTerm) ||
                    c.Player.Sponsor.Name.ToLower().StartsWith(searchTerm) ||
                    c.Player.User.Sponsor.Name.ToLower().StartsWith(searchTerm) ||
                    c.Player.User.Name.ToLower().Contains(searchTerm)
            )
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.StartTime,
                c.EndTime,
                c.SpecId,
                c.Points,
                c.Score,
                User = new PlayerWithSponsor
                {
                    Id = c.Player.UserId,
                    Name = c.Player.User.ApprovedName,
                    Sponsor = new SimpleSponsor
                    {
                        Id = c.Player.User.SponsorId,
                        Name = c.Player.User.Sponsor.Name,
                        Logo = c.Player.User.Sponsor.Logo
                    }
                }
            })
            .GroupBy(c => c.User)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.GroupBy(c => c.SpecId).ToDictionary(c => c.Key, c => c), cancellationToken);

        var specIds = users.Values.SelectMany(u => u.Keys).Distinct().ToArray();
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => specIds.Contains(cs.Id))
            .Select(cs => new
            {
                cs.Id,
                cs.Name,
                cs.Tag
            })
            .ToDictionaryAsync(cs => cs.Id, cs => cs, cancellationToken);

        var game = await _store
            .WithNoTracking<Data.Game>()
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .SingleOrDefaultAsync(g => g.Id == request.GameId, cancellationToken);

        var responseUsers = users.Select(u =>
            {
                var challengeSpecs = new List<GameCenterPracticeContextChallengeSpec>();
                var totalAttempts = 0;
                var activeChallenge = default(SimpleEntity);
                var activeChallengeEndTimestamp = default(long?);

                foreach (var specId in u.Value.Keys)
                {
                    var attemptCount = u.Value[specId].Count();
                    totalAttempts += attemptCount;
                    var lastAttempt = u.Value[specId].OrderByDescending(c => c.StartTime).FirstOrDefault();
                    var bestAttempt = u.Value[specId].OrderByDescending(c => c.Score).FirstOrDefault();

                    if (activeChallenge is null)
                    {
                        var potentialActiveChallenge = u.Value[specId]
                            .Where(c => c.StartTime > DateTimeOffset.MinValue && c.StartTime <= nowish)
                            .Where(c => c.EndTime == DateTimeOffset.MinValue || c.EndTime > nowish)
                            .FirstOrDefault();

                        if (potentialActiveChallenge is not null)
                        {
                            activeChallenge = new SimpleEntity { Id = potentialActiveChallenge.Id, Name = potentialActiveChallenge.Name };
                            activeChallengeEndTimestamp = potentialActiveChallenge.EndTime.IsEmpty() ? null : potentialActiveChallenge.EndTime.ToUnixTimeMilliseconds();
                        }
                    }
                    specs.TryGetValue(specId, out var spec);

                    challengeSpecs.Add(new()
                    {
                        Id = specId,
                        Name = bestAttempt.Name,
                        Tag = spec?.Tag,
                        AttemptCount = attemptCount,
                        BestAttempt = bestAttempt is null ? null : new GameCenterPracticeContextChallengeAttempt
                        {
                            AttemptTimestamp = bestAttempt.StartTime.ToUnixTimeMilliseconds(),
                            Result = _scoringService.GetChallengeResult(bestAttempt.Score, bestAttempt.Points),
                            Score = bestAttempt.Score
                        },
                        LastAttemptDate = lastAttempt.StartTime.IsEmpty() ? null : lastAttempt.StartTime.ToUnixTimeMilliseconds(),
                    });
                }

                return new GameCenterPracticeContextUser
                {
                    Id = u.Key.Id,
                    Name = u.Key.Name,
                    Sponsor = u.Key.Sponsor,
                    ActiveChallenge = activeChallenge,
                    ActiveChallengeEndTimestamp = activeChallengeEndTimestamp,
                    TotalAttempts = totalAttempts,
                    UniqueChallengeSpecs = u.Value.Keys.Count,
                    ChallengeSpecs = challengeSpecs
                };
            })
            .Where(u => request.SessionStatus is null || (request.SessionStatus == GameCenterPracticeSessionStatus.Playing == u.ActiveChallenge is not null));

        responseUsers = request.Sort switch
        {
            GameCenterPracticeSort.AttemptCount => responseUsers.OrderByDescending(u => u.TotalAttempts),
            _ => responseUsers.OrderBy(u => u.Name),
        };

        return new GameCenterPracticeContext
        {
            Game = game,
            Users = responseUsers
        };
    }
}
