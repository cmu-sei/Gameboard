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
