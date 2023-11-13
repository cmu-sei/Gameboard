using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeGraderUrlService
{
    public string BuildGraderUrl();
}

internal class ChallengeGraderUrlService : IChallengeGraderUrlService
{
    private readonly IAppUrlService _appUrlService;

    public ChallengeGraderUrlService(IAppUrlService appUrlService)
    {
        _appUrlService = appUrlService;
    }

    public string BuildGraderUrl()
    {
        return _appUrlService.ToAppAbsoluteUrl("api/challenge/grade");
    }
}
