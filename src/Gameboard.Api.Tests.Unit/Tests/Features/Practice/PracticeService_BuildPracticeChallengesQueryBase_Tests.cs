// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Unit;

public class PracticeService_BuildPracticeChallengesQueryBase_Tests
{
    [Theory, GameboardAutoData]
    public async Task SearchPracticeChallenges_WithDisabled_ReturnsEmpty(IFixture fixture)
    {
        // given a challenge which is disabled
        var disabledSpec = new Data.ChallengeSpec
        {
            Id = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            Text = fixture.Create<string>(),
            Tags = fixture.Create<string>(),
            Disabled = true,
            Game = new Data.Game
            {
                Name = fixture.Create<string>(),
                PlayerMode = PlayerMode.Practice
            }
        };

        var sut = GetSutWithResults(disabledSpec);

        // when a query for all challenges is issued
        var query = await sut.GetPracticeChallengesQueryBase(string.Empty);
        var result = await query.ToArrayAsync(CancellationToken.None);

        // then we expect no results
        result.Length.ShouldBe(0);
    }

    [Theory, GameboardAutoData]
    public async Task SearchPracticeChallenges_WithEnabled_Returns(IFixture fixture)
    {
        var enabledSpec = new Data.ChallengeSpec
        {
            Id = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            Text = fixture.Create<string>(),
            Tags = fixture.Create<string>(),
            Disabled = false,
            Game = new Data.Game
            {
                IsPublished = true,
                Name = fixture.Create<string>(),
                PlayerMode = PlayerMode.Practice
            }
        };

        var sut = GetSutWithResults(enabledSpec);

        // when a query for all challenges is issued
        var query = await sut.GetPracticeChallengesQueryBase(string.Empty);
        var result = await query.ToArrayAsync(CancellationToken.None);

        // then we expect one result
        result.Length.ShouldBe(1);
    }

    private PracticeService GetSutWithResults(params Data.ChallengeSpec[] specs)
    {
        var queryResults = specs.BuildMock();

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.ChallengeSpec>())
            .WithAnyArguments()
            .Returns(queryResults);

        return new PracticeService
        (
            new CoreOptions(),
            A.Fake<IGuidService>(),
            A.Fake<ILockService>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            A.Fake<IUserRolePermissionsService>(),
            A.Fake<ISlugService>(),
            store
        );
    }
}
