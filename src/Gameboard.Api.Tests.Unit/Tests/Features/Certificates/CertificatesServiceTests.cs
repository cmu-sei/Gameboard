// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class CertificatesServiceTests
{
    private readonly CertificatesService _sut;

    public CertificatesServiceTests()
    {
        _sut = new CertificatesService
        (
            A.Fake<CoreOptions>(),
            A.Fake<INowService>(),
            A.Fake<IStore>(),
            A.Fake<ITeamService>()
        );
    }

    [Fact]
    public void GetDurationDescription_WithHoursAndMinutes_ResolvesExpected()
    {
        // given
        var duration = TimeSpan.FromMinutes(62);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 2 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHours_HidesMinutes()
    {
        // given
        var duration = TimeSpan.FromHours(2);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("2 hours");
    }

    [Fact]
    public void GetDurationDescription_WithMinutes_HidesHours()
    {
        // given
        var duration = TimeSpan.FromMinutes(39);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("39 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHms_HidesSeconds()
    {
        // given
        // 1 hour, 3 minutes, 4 seconds
        var duration = TimeSpan.FromSeconds(3787);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 3 minutes");
    }

    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScoreZero_ReturnsEmptyArray
    (
        string certificateTemplateId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        // when a team scores 0
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IStore>();
        var fakePlayers = new Data.Player[]
        {
            new()
            {
                PartialCount = 0,
                Game = new Data.Game
                {
                    Id = gameId,
                    CertificateTemplateId = certificateTemplateId,
                    GameEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(1)
                },
                Score = 1,
                SessionEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(2),
                TeamId = teamId,
                UserId = userId,
                User = new Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        var fakeScores = new DenormalizedTeamScore[]
        {
            new()
            {
                GameId = gameId,
                CumulativeTimeMs = 100,
                ScoreAdvanced = 0,
                ScoreAutoBonus = 0,
                ScoreChallenge = 0,
                ScoreManualBonus = 0,
                SolveCountNone = 0,
                SolveCountPartial = 1,
                SolveCountComplete = 0,
                TeamId = teamId,
                TeamName = null,
                Rank = 1,
                ScoreOverall = 0
            }
        }.BuildMock();

        A.CallTo(() => fakeStore.WithNoTracking<Data.Player>()).Returns(fakePlayers);
        A.CallTo(() => fakeStore.WithNoTracking<DenormalizedTeamScore>()).Returns(fakeScores);

        var sut = new CertificatesService
        (
            A.Fake<CoreOptions>(),
            new NowService(),
            fakeStore,
            A.Fake<ITeamService>()
        );
        // act
        var result = await sut.GetCompetitiveCertificates(userId, CancellationToken.None);

        // assert
        result.Count().ShouldBe(0);
    }

    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScore1_ReturnsOneCertificate
    (
        string certificateTemplateId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        // arrange
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IStore>();
        var fakePlayers = new Data.Player[]
        {
            new()
            {
                PartialCount = 0,
                Game = new Data.Game
                {
                    Id = gameId,
                    CertificateTemplateId = certificateTemplateId,
                    GameEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(1)
                },
                Score = 1,
                SessionEnd = DateTimeOffset.UtcNow - TimeSpan.FromDays(2),
                TeamId = teamId,
                UserId = userId,
                User = new Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        var fakeScores = new DenormalizedTeamScore[]
        {
            new()
            {
                GameId = gameId,
                CumulativeTimeMs = 100,
                ScoreAdvanced = 0,
                ScoreAutoBonus = 0,
                ScoreChallenge = 1,
                ScoreManualBonus = 0,
                SolveCountNone = 0,
                SolveCountPartial = 1,
                SolveCountComplete = 0,
                TeamId = teamId,
                TeamName = null,
                Rank = 1,
                ScoreOverall = 1
            }
        }.BuildMock();

        A.CallTo(() => fakeStore.WithNoTracking<Data.Player>()).Returns(fakePlayers);
        A.CallTo(() => fakeStore.WithNoTracking<DenormalizedTeamScore>()).Returns(fakeScores);

        var sut = new CertificatesService
        (
            A.Fake<CoreOptions>(),
            new NowService(),
            fakeStore,
            A.Fake<ITeamService>()
        );
        // act
        var result = await sut.GetCompetitiveCertificates(userId, CancellationToken.None);

        // assert
        result.Count().ShouldBe(1);
    }
}
