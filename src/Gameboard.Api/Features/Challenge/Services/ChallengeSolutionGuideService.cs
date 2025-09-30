// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeSolutionGuideService
{
    Task<ChallengeSolutionGuide> GetSolutionGuide(string challengeSpecId, CancellationToken cancellationToken);
    Task<ChallengeSolutionGuide> GetSolutionGuide(Data.ChallengeSpec challengeSpec, PlayerMode playerMode);
    Task<ChallengeSolutionGuide> GetSolutionGuide(string challengeSpecId, string solutionGuideUrl, PlayerMode playerMode, bool showInCompetitiveMode);
}

internal class ChallengeSolutionGuideService(IStore store) : IChallengeSolutionGuideService
{
    private readonly IStore _store = store;

    public async Task<ChallengeSolutionGuide> GetSolutionGuide(string challengeSpecId, CancellationToken cancellationToken)
    {
        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Select(s => new
            {
                s.Id,
                s.Game.PlayerMode,
                s.ShowSolutionGuideInCompetitiveMode,
                s.SolutionGuideUrl
            })
            .SingleAsync(s => s.Id == challengeSpecId, cancellationToken);

        return await GetSolutionGuide(spec.Id, spec.SolutionGuideUrl, spec.PlayerMode, spec.ShowSolutionGuideInCompetitiveMode);
    }

    public Task<ChallengeSolutionGuide> GetSolutionGuide(Data.ChallengeSpec challengeSpec, PlayerMode playerMode)
        => GetSolutionGuide(challengeSpec.Id, challengeSpec.SolutionGuideUrl, playerMode, challengeSpec.ShowSolutionGuideInCompetitiveMode);

    public Task<ChallengeSolutionGuide> GetSolutionGuide(string challengeSpecId, string solutionGuideUrl, PlayerMode playerMode, bool showInCompetitiveMode)
    {
        if (solutionGuideUrl.IsNotEmpty() && playerMode == PlayerMode.Practice || showInCompetitiveMode)
        {
            return Task.FromResult(new ChallengeSolutionGuide
            {
                ChallengeSpecId = challengeSpecId,
                ShowInCompetitiveMode = showInCompetitiveMode,
                Url = solutionGuideUrl
            });
        }

        return null;
    }
}
