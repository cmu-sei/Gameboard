using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetGameCenterPracticeContextQuery(string GameId) : IRequest<GameCenterPracticeContext>;

internal class GetGameCenterPracticeQueryHandler : IRequestHandler<GetGameCenterPracticeContextQuery, GameCenterPracticeContext>
{
    private readonly EntityExistsValidator<GetGameCenterPracticeContextQuery, Data.Game> _gameExists;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetGameCenterPracticeContextQuery> _validatorService;

    public GetGameCenterPracticeQueryHandler
    (
        EntityExistsValidator<GetGameCenterPracticeContextQuery, Data.Game> gameExists,
        IScoringService scoringService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetGameCenterPracticeContextQuery> validatorService
    )
    {
        _gameExists = gameExists;
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
        var users = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.GameId == request.GameId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.StartTime,
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

        return new GameCenterPracticeContext
        {
            Users = users.Select(u =>
            {
                var challenges = new List<GameCenterPracticeContextChallenge>();

                foreach (var spec in u.Value)
                {
                    var lastAttempt = spec.Value.OrderByDescending(c => c.StartTime).FirstOrDefault();
                    var bestAttempt = spec.Value.OrderByDescending(c => c.Score).FirstOrDefault();

                    challenges.Add(new()
                    {
                        Id = lastAttempt.Id,
                        ChallengeSpec = new SimpleEntity { Id = spec.Key, Name = lastAttempt.Name },
                        AttemptCount = spec.Value.Count(),
                        LastAttemptDate = lastAttempt?.StartTime.ToUnixTimeMilliseconds(),
                        BestAttempt = bestAttempt is null ? null : new GameCenterPracticeContextChallengeAttempt
                        {
                            AttemptTimestamp = bestAttempt.StartTime.ToUnixTimeMilliseconds(),
                            Result = _scoringService.GetChallengeResult(bestAttempt.Score, bestAttempt.Points),
                            Score = bestAttempt.Score
                        }

                    });
                }

                return new GameCenterPracticeContextUser
                {
                    Id = u.Key.Id,
                    Name = u.Key.Name,
                    Sponsor = u.Key.Sponsor,
                    Challenges = challenges
                };
            })
        };
    }
}
