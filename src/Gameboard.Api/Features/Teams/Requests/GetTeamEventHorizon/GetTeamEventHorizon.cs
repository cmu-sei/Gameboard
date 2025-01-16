using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record GetTeamEventHorizonQuery(string TeamId) : IRequest<EventHorizon>;

internal class GetTeamEventHorizonHandler
(
    IActingUserService actingUserService,
    IGuidService guidService,
    IJsonService jsonService,
    IScoringService scoringService,
    IStore store,
    TeamExistsValidator<GetTeamEventHorizonQuery> teamExists,
    ITeamService teamService,
    IValidatorService<GetTeamEventHorizonQuery> validator
) : IRequestHandler<GetTeamEventHorizonQuery, EventHorizon>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IGuidService _guidService = guidService;
    private readonly IJsonService _jsonService = jsonService;
    private readonly IScoringService _scoring = scoringService;
    private readonly IStore _store = store;
    private readonly TeamExistsValidator<GetTeamEventHorizonQuery> _teamExists = teamExists;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<GetTeamEventHorizonQuery> _validator = validator;

    public async Task<EventHorizon> Handle(GetTeamEventHorizonQuery request, CancellationToken cancellationToken)
    {
        // validate
        var actingUserId = _actingUserService.Get().Id;

        await _validator
            .Auth(config => config
                .Require(PermissionKey.Teams_Observe)
                .Unless
                (
                    () => _store
                        .WithNoTracking<Data.Player>()
                        .Where(p => p.TeamId == request.TeamId && p.UserId == actingUserId)
                        .AnyAsync(),
                    new UserIsntOnTeam(actingUserId, request.TeamId)
                )
            )
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .Validate(request, cancellationToken);

        // and awaaaaay we go
        // pull sources for events
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Events)
            .Include(c => c.Game)
            .Include(c => c.Submissions)
            .Where(c => c.TeamId == request.TeamId)
            .ToArrayAsync(cancellationToken);

        if (challenges.Length == 0)
            return null;

        // make sure we have exactly one game
        var games = challenges.Select(c => c.Game).ToArray();
        if (games.Select(g => g.Id).Distinct().Count() > 1)
            throw new Exception($"Team has {request.TeamId} challenges in multiple games.");
        var game = games.First();

        // and exactly one captain
        var captain = await _teamService.ResolveCaptain(request.TeamId, cancellationToken);

        // have to pull specs separately because of foreign key silliness
        var specIds = challenges.Select(c => c.SpecId).ToArray();
        var challengeSpecs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => specIds.Contains(cs.Id))
            .ToArrayAsync(cancellationToken);

        // and we want their scores so we can note that for the challenge
        var score = await _scoring.GetTeamScore(request.TeamId, cancellationToken);

        // manually compose the events since they come from different sources
        var events = new List<IEventHorizonEvent>();

        // Started comes from challenge table
        // we use the Challenges table rather than an equivalent event in ChallengeEvents
        // because that's the value we typically send when the client asks for a challenge's
        // start time
        var challengeStartedEvents = challenges
            .Select(c => new EventHorizonEvent
            {
                // we present these with an Id because the client needs to be able
                // to differentiate between events, but we don't have a real representation of this
                Id = _guidService.Generate(),
                ChallengeId = c.Id,
                Type = EventHorizonEventType.ChallengeStarted,
                Timestamp = c.StartTime
            });
        events.AddRange(challengeStartedEvents);

        // gamespace on/off comes from ChallengeEvents
        var gamespaceEvents = BuildGamespaceEvents(challenges);

        events.AddRange(gamespaceEvents);

        // submission/solve events come from ChallengeSubmissions
        var challengeMaxScores = challenges.ToDictionary(c => c.Id, c => (double)c.Points);
        var submissions = challenges.SelectMany(c => c.Submissions).ToArray();
        var submissionEvents = BuildSubmissionEvents(submissions, challengeMaxScores);
        events.AddRange(submissionEvents);

        // finally, build the event horizon response
        return new EventHorizon
        {
            Game = new EventHorizonGame
            {
                Id = game.Id,
                Name = game.Name,
                ChallengeSpecs = challengeSpecs.Select(s => new EventHorizonChallengeSpec
                {
                    Id = s.Id,
                    Name = s.Name,
                    MaxAttempts = game.MaxAttempts,
                    MaxPossibleScore = s.Points
                })
            },
            Team = new EventHorizonTeam
            {
                Id = request.TeamId,
                Name = captain.ApprovedName,
                Challenges = challenges.Select(c => new EventHorizonTeamChallenge
                {
                    Id = c.Id,
                    Score = score.Challenges.Where(challenge => challenge.Id == c.Id).FirstOrDefault()?.Score?.TotalScore ?? 0,
                    SpecId = c.SpecId,
                }),
                Session = new EventHorizonSession
                {
                    Start = captain.SessionBegin,
                    End = captain.SessionEnd.IsEmpty() ? null : captain.SessionEnd,
                },
                Events = events
            }
        };
    }

    /// <summary>
    /// Maps a GamespaceOn/GamespaceOff entry from the ChallengeEvents table to an EventHorizonEvent.
    /// </summary>
    /// <param name="challenges"></param>
    /// <returns></returns>
    private IEnumerable<EventHorizonGamespaceOnOffEvent> BuildGamespaceEvents(IEnumerable<Data.Challenge> challenges)
    {
        var retVal = new List<EventHorizonGamespaceOnOffEvent>();

        // we grab the start/end times just in case there's a gamespace Off event before any On events
        // or vice versa (and autofill with on with the challenge start/end)
        var challengeStartEndTimes = challenges
            .ToDictionary(c => c.Id, c => new { c.StartTime, c.EndTime });

        // group and order the events so we can match up Start/On with Off
        var events = challenges
            .SelectMany(c => c.Events)
            .Where(e => e.Type == ChallengeEventType.Started || e.Type == ChallengeEventType.GamespaceOn || e.Type == ChallengeEventType.GamespaceOff)
            .GroupBy(e => e.ChallengeId)
            .ToDictionary(gr => gr.Key, gr => gr.ToArray());

        foreach (var challengeId in events.Keys)
        {
            Data.ChallengeEvent lastOnEvent = null;

            foreach (var gamespaceEvent in events[challengeId].OrderBy(e => e.Timestamp))
            {
                if (gamespaceEvent.Type == ChallengeEventType.GamespaceOn || gamespaceEvent.Type == ChallengeEventType.Started)
                    lastOnEvent = gamespaceEvent;
                else if (gamespaceEvent.Type == ChallengeEventType.GamespaceOff)
                {
                    // it's technically possible for a challenge to have a gamespace off event but no on event.
                    // if so, make the start time the challenge's start.
                    var lastOnTimestamp = lastOnEvent?.Timestamp ?? challengeStartEndTimes[challengeId].StartTime;

                    retVal.Add(new EventHorizonGamespaceOnOffEvent
                    {
                        Id = lastOnEvent?.Id ?? gamespaceEvent.Id,
                        ChallengeId = challengeId,
                        Timestamp = lastOnTimestamp,
                        Type = EventHorizonEventType.GamespaceOnOff,
                        EventData = new EventHorizonGamespaceOnOffEventData
                        {
                            OffAt = gamespaceEvent.Timestamp
                        }
                    });

                    lastOnEvent = null;
                }
            }

            // if we have a straggling "on" event, pretend it ends at the end of the challenge window
            if (lastOnEvent is not null)
            {
                retVal.Add(new EventHorizonGamespaceOnOffEvent
                {
                    Id = lastOnEvent.Id,
                    ChallengeId = lastOnEvent.ChallengeId,
                    Timestamp = lastOnEvent.Timestamp,
                    Type = EventHorizonEventType.GamespaceOnOff,
                    EventData = new EventHorizonGamespaceOnOffEventData { OffAt = challengeStartEndTimes[lastOnEvent.ChallengeId].EndTime }
                });
            }
        }

        return retVal.OrderBy(e => e.Timestamp).ToArray();
    }

    private IEnumerable<IEventHorizonEvent> BuildSubmissionEvents(IEnumerable<ChallengeSubmission> submissions, IDictionary<string, double> challengeSolveScores)
    {
        // we'll return a list of submission/solve events
        var retVal = new List<IEventHorizonEvent>();

        // the submission events care about which attempt number this is and how many total attempts they used,
        // so compute those first
        var submissionAttemptCounts = submissions
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.Count());

        // we also have to do things like order/rank the submissions to determine which
        // attempt happened in each event
        var orderedSubmissions = submissions
           .OrderBy(s => s.ChallengeId)
               .ThenBy(s => s.SubmittedOn)
           .ToArray();

        var currentChallengeId = "";
        var currentSubmissionRank = 0;

        foreach (var submission in orderedSubmissions)
        {
            if (submission.ChallengeId != currentChallengeId)
            {
                currentChallengeId = submission.ChallengeId;
                currentSubmissionRank = 0;
            }

            currentSubmissionRank += 1;

            if (submission.Score >= challengeSolveScores[submission.ChallengeId])
                retVal.Add(new EventHorizonSolveCompleteEvent
                {
                    Id = submission.Id,
                    ChallengeId = submission.ChallengeId,
                    Timestamp = submission.SubmittedOn,
                    Type = EventHorizonEventType.SolveComplete,
                    EventData = new()
                    {
                        AttemptsUsed = currentSubmissionRank,
                        FinalScore = submission.Score
                    }
                });
            else
            {
                var answers = _jsonService.Deserialize<ChallengeSubmissionAnswers>(submission.Answers);

                // read the answers from the JSON blob
                retVal.Add(new EventHorizonSubmissionScoredEvent
                {
                    Id = submission.Id,
                    ChallengeId = submission.ChallengeId,
                    Timestamp = submission.SubmittedOn,
                    Type = EventHorizonEventType.SubmissionScored,
                    EventData = new()
                    {
                        Answers = answers.Answers,
                        AttemptNumber = currentSubmissionRank,
                        Score = submission.Score
                    }
                });
            }
        }

        return retVal;
    }
}
