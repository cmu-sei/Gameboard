using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Test;

public class UnityGameServiceTests
{
    [Fact]
    public void GetMissionCompleteDefinitionString_Matches_IsMissionComplete()
    {
        // arrange
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var factory = serviceProvider.GetService<ILoggerFactory>();
        var logger = factory?.CreateLogger<UnityGameService>();

        var service = new UnityGameService(
            logger,
            Substitute.For<IMapper>(),
            Substitute.For<CoreOptions>(),
            Substitute.For<IChallengeStore>(),
            Substitute.For<IUnityStore>(),
            Substitute.For<ITopoMojoApiClient>(),
            Substitute.For<ConsoleActorMap>()
        );
        var regex = service.GetMissionCompleteEventRegex();


        // act
        var missionCompleteString = service.GetMissionCompleteDefinitionString("secret-mission");
        var match = regex.IsMatch(missionCompleteString);

        // assert
        Assert.True(match);
    }
}
