using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public class ChallengeScoredEventArgs
{
    public required Data.Challenge Challenge { get; set; }
}

public interface IPracticeChallengeScoringListener
{
    Task NotifyChallengeScored(Data.Challenge challenge, CancellationToken cancellationToken);
    Task NotifyAttemptsExhausted(Data.Challenge challenge, CancellationToken cancellationToken);
    Task<Api.Player> AdjustSessionEnd(SessionChangeRequest model, User actor, CancellationToken cancellationToken);
}

internal class PracticeChallengeScoringListener : IPracticeChallengeScoringListener
{
    private IActingUserService _actingUser;
    private IGameEngineService _gameEngine;
    private IInternalHubBus _hubBus;
    private IMapper _mapper;
    private IPlayerStore _playerStore;
    private IPracticeService _practiceService;

    public PracticeChallengeScoringListener
    (
        IActingUserService actingUser,
        IGameEngineService gameEngine,
        IInternalHubBus hubBus,
        IMapper mapper,
        IPlayerStore playerStore,
        IPracticeService practiceService
    )
    {
        _actingUser = actingUser;
        _gameEngine = gameEngine;
        _hubBus = hubBus;
        _mapper = mapper;
        _playerStore = playerStore;
        _practiceService = practiceService;
    }

    public Task NotifyChallengeScored(Data.Challenge challenge, CancellationToken cancellationToken)
    {
        return AdjustSessionEnd(new SessionChangeRequest
        {
            TeamId = challenge.TeamId,
            SessionEnd = DateTimeOffset.MinValue
        }, _actingUser.Get(), cancellationToken);
    }

    public Task NotifyAttemptsExhausted(Data.Challenge challenge, CancellationToken cancellationToken)
    {
        return AdjustSessionEnd(new SessionChangeRequest
        {
            TeamId = challenge.TeamId,
            SessionEnd = DateTimeOffset.MinValue
        }, _actingUser.Get(), cancellationToken);
    }

    public async Task<Api.Player> AdjustSessionEnd(SessionChangeRequest model, User actor, CancellationToken cancellationToken)
    {
        var team = await _playerStore.ListTeam(model.TeamId).ToArrayAsync(cancellationToken);
        var sudo = actor.IsRegistrar;

        var manager = team.FirstOrDefault(p => p.Role == PlayerRole.Manager);

        if (sudo.Equals(false) && manager.IsCompetition)
            throw new ActionForbidden();

        // auto increment for practice sessions
        if (manager.IsPractice)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var settings = await _practiceService.GetSettings(cancellationToken);

            // end session now or extend by one hour (hard value for now, added to practice settings later)
            model.SessionEnd = model.SessionEnd.Year == 1
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.UtcNow.AddMinutes(60)
            ;
            if (settings.MaxPracticeSessionLengthMinutes.HasValue)
            {
                var maxTime = manager.SessionBegin.AddMinutes(settings.MaxPracticeSessionLengthMinutes.Value);
                if (model.SessionEnd > maxTime)
                    model.SessionEnd = maxTime;
            }
        }

        foreach (var player in team)
            player.SessionEnd = model.SessionEnd;

        await _playerStore.Update(team);

        // push gamespace extension
        var changes = await _playerStore.DbContext.Challenges
            .Where(c => c.TeamId == manager.TeamId)
            .Select(c => _gameEngine.ExtendSession(c, model.SessionEnd))
            .ToArrayAsync();

        await Task.WhenAll(changes);

        var mappedManager = _mapper.Map<Api.Player>(manager);
        await _hubBus.SendTeamUpdated(mappedManager, actor);
        return mappedManager;
    }
}
