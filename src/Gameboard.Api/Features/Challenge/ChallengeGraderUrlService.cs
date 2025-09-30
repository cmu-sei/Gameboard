// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeGraderUrlService
{
    public string BuildGraderUrl();
}

internal class ChallengeGraderUrlService(IAppUrlService appUrlService) : IChallengeGraderUrlService
{
    private readonly IAppUrlService _appUrlService = appUrlService;

    public string BuildGraderUrl()
    {
        return _appUrlService.ToAppAbsoluteUrl("api/challenge/grade");
    }
}
