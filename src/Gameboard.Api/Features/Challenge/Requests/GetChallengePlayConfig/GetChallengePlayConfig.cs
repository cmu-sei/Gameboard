// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public record GetChallengePlayConfigQuery(string ChallengeId) : IRequest<GetChallengePlayConfigResponse>;

internal sealed class GetChallengePlayConfigHandler(
    IActingUserService actingUserService,
    EntityExistsValidator<Data.Challenge> challengeExists,
    IChallengeSolutionGuideService challengeSolutionGuideService,
    IGameEngineService gameEngine,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
) : IRequestHandler<GetChallengePlayConfigQuery, GetChallengePlayConfigResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists = challengeExists;
    private readonly IChallengeSolutionGuideService _challengeSolutionGuideService = challengeSolutionGuideService;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetChallengePlayConfigResponse> Handle(GetChallengePlayConfigQuery request, CancellationToken cancellationToken)
    {
        var actingUserId = _actingUserService.Get().Id;

        await _validatorService
            .Auth
            (
                config => config
                    .Require(Users.PermissionKey.Teams_Observe)
                    .Unless(async () =>
                    {
                        var challengeTeamId = await _store
                            .WithNoTracking<Data.Challenge>()
                            .Where(c => c.Id == request.ChallengeId)
                            .Select(c => c.TeamId)
                            .SingleAsync(cancellationToken);


                        return await _teamService.IsOnTeam(challengeTeamId, actingUserId);
                    })
            )
            .AddValidator(_challengeExists.UseValue(request.ChallengeId))
            .Validate(cancellationToken);

        var challengeData = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == request.ChallengeId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.StartTime,
                c.EndTime,
                c.GameEngineType,
                c.PlayerMode,
                c.Points,
                c.Score,
                c.SpecId,
                c.State,
                c.TeamId
            })
            .SingleAsync(cancellationToken);

        var challengeState = await _gameEngine.GetChallengeState(challengeData.GameEngineType, challengeData.State);

        return new GetChallengePlayConfigResponse
        {
            Config = new ChallengePlayConfig
            {
                Challenge = new SimpleEntity { Id = challengeData.Id, Name = challengeData.Name },

                AttemptsMax = challengeState.Challenge.MaxAttempts,
                AttemptsUsed = challengeState.Challenge.Attempts,
                IsPractice = challengeData.PlayerMode == PlayerMode.Practice,
                Score = challengeData.Score,
                ScoreMax = challengeData.Points,
                SolutionGuide = await _challengeSolutionGuideService.GetSolutionGuide(challengeData.SpecId, cancellationToken),
                TimeEnd = challengeData.StartTime.ToUniversalTime().ToUnixTimeMilliseconds(),
                TimeStart = challengeData.StartTime.ToUniversalTime().ToUnixTimeMilliseconds(),
                TeamId = challengeData.TeamId,

                Sections = [new ChallengePlayConfigSection
                {
                    Name = challengeData.Name,
                    PreReqPrevSection = 0,
                    PreReqTotal = 0,
                    Questions = challengeState.Challenge.Questions.Select(q => new ChallengePlayConfigQuestion
                    {
                        PreviousResponses = [],
                        SampleAnswer = q.Hint,
                        Text = q.Text
                    })
                }]
                // Sections = challengeState.Challenge(s => new ChallengePlayConfigSection
                // {
                //     Name = s.,
                //     PreReqPrevSection = s.PreReqPrevSection,
                //     PreReqTotal = s.PreReqTotal,
                //     Questions = s.Questions.Select(q => new ChallengePlayConfigQuestion
                //     {
                //         PreviousResponses = [],
                //         SampleAnswer = q.Hint,
                //         Text = q.Text
                //     })
                // })
            }
        };
    }
}
