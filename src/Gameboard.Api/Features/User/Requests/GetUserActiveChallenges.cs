using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Consoles;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record GetUserActiveChallengesQuery(string UserId) : IRequest<GetUserActiveChallengesResponse>;

public sealed class GetUserActiveChallengesResponse
{
    public required SimpleEntity User { get; set; }
    public required IEnumerable<UserActiveChallenge> Challenges { get; set; }
}

internal class GetUserActiveChallengesHandler
(
    CoreOptions coreOptions,
    IGameEngineService gameEngine,
    INowService now,
    IStore store,
    EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> userExists,
    IValidatorService<GetUserActiveChallengesQuery> validator
) : IRequestHandler<GetUserActiveChallengesQuery, GetUserActiveChallengesResponse>
{
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly EntityExistsValidator<GetUserActiveChallengesQuery, Data.User> _userExists = userExists;
    private readonly IValidatorService<GetUserActiveChallengesQuery> _validator = validator;

    public async Task<GetUserActiveChallengesResponse> Handle(GetUserActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _validator
            .Auth
            (
                a => a
                    .Require(PermissionKey.Teams_Observe)
                    .UnlessUserIdIn(request.UserId)
            )
            .AddValidator(_userExists.UseProperty(m => m.UserId))
            .Validate(request, cancellationToken);

        // retrieve stuff (initial pull from DB side eval)
        var user = await _store
            .WithNoTracking<Data.User>()
            .Select(u => new SimpleEntity { Id = u.Id, Name = u.ApprovedName })
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Player.SessionBegin >= DateTimeOffset.MinValue)
            .Where(c => c.Player.SessionEnd > _now.Get())
            .Where(c => c.Player.UserId == request.UserId)
            .OrderByDescending(c => c.Player.SessionEnd)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SpecId,
                Game = new SimpleEntity { Id = c.GameId, Name = c.Game.Name },
                c.Game.ChallengesFeedbackTemplateId,
                c.GameEngineType,
                EndTime = c.EndTime == DateTimeOffset.MinValue ? default(DateTimeOffset?) : c.EndTime,
                c.State,
                c.PlayerMode,
                c.TeamId,
                c.Score,
                c.Points
            })
            .ToArrayAsync(cancellationToken);

        var challengeScoreAttemptsStates = new Dictionary<string, UserActiveChallengeScoreAndAttemptsState>();
        var challengeStates = new Dictionary<string, GameEngineGameState>();
        var teamIds = new List<string>();
        var challengeConsoleIds = new List<ConsoleId>();

        foreach (var challenge in challenges)
        {
            var state = await _gameEngine.GetChallengeState(challenge.GameEngineType, challenge.State);
            challengeStates.Add(challenge.Id, state);
            challengeScoreAttemptsStates.Add(challenge.Id, new UserActiveChallengeScoreAndAttemptsState
            {
                Attempts = state.Challenge?.Attempts ?? 0,
                MaxAttempts = (state.Challenge?.MaxAttempts ?? 0) > 0 ? state.Challenge.MaxAttempts : null,
                Score = (decimal)Math.Round(challenge.Score),
                MaxPossibleScore = challenge.Points
            });
            challengeConsoleIds.AddRange([.. state.Vms.Select(vm => new ConsoleId { ChallengeId = challenge.Id, Name = vm.Name })]);
            teamIds.Add(challenge.TeamId);
        }

        // load team data
        teamIds = [.. teamIds.Distinct()];
        var teams = await _store.WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .Where(p => p.Role == PlayerRole.Manager)
            .Select(p => new SimpleEntity { Id = p.TeamId, Name = p.ApprovedName })
            .GroupBy(p => p.Id)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.FirstOrDefault()?.Name, cancellationToken);

        // get VM access
        var vmConsoles = await _gameEngine.GetConsoles(coreOptions.GameEngineType, [.. challengeConsoleIds], cancellationToken);
        var challengeVmConsoles = vmConsoles.GroupBy(vm => vm.Id.ChallengeId).ToDictionary(gr => gr.Key, gr => gr.ToArray());

        var typedChallenges = challenges.Select(c => new UserActiveChallenge
        {
            Id = c.Id,
            Name = c.Name,
            Game = c.Game,
            Spec = new SimpleEntity { Id = c.SpecId, Name = c.Name },
            Team = new SimpleEntity { Id = c.TeamId, Name = teams[c.TeamId] },
            FeedbackTemplateId = c.ChallengesFeedbackTemplateId,
            Mode = c.PlayerMode,
            EndsAt = c.EndTime?.ToUnixTimeMilliseconds() ?? null,
            IsDeployed = challengeStates[c.Id].Vms?.Any() ?? false,
            Markdown = challengeStates[c.Id].Markdown,
            ScoreAndAttemptsState = challengeScoreAttemptsStates[c.Id],
            Vms = challengeVmConsoles[c.Id].Select(consoleState => new UserActiveChallengeVm
            {
                Id = consoleState.Id.ToString(),
                Name = consoleState.Id.Name,
                AccessTicket = consoleState.AccessTicket,
                Url = consoleState.Url
            })
        })
        // now that we have info about points and attempts, we can reason about whether we should return
        // challenges that are maxed out on score or guesses
        .Where(c => c.ScoreAndAttemptsState.Score < c.ScoreAndAttemptsState.MaxPossibleScore)
        .Where(c => c.ScoreAndAttemptsState.MaxAttempts is null || c.ScoreAndAttemptsState.Attempts < c.ScoreAndAttemptsState.MaxAttempts);

        return new GetUserActiveChallengesResponse
        {
            User = user,
            Challenges = typedChallenges
        };
    }
}
