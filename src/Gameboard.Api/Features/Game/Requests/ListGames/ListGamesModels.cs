using System;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Games.Requests;

public enum ListGamesExecutionStatus
{
    Future,
    Live,
    Ongoing,
    Past
}

public sealed class ListGamesResponse
{
    public required ListGamesResponseGame[] Games { get; set; }
}

public sealed class ListGamesResponseGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
    public required string Division { get; set; }
    public required string Logo { get; set; }
    public required string SponsorId { get; set; }
    public required string Background { get; set; }
    public required DateTimeOffset? GameStart { get; set; }
    public required DateTimeOffset? GameEnd { get; set; }
    public required ListGameResponseGameRegistration Registration { get; set; }
    public required int MinTeamSize { get; set; } = 1;
    public required int MaxTeamSize { get; set; } = 1;
    public required int MaxAttempts { get; set; } = 0;
    public required int SessionMinutes { get; set; } = 60;
    public required int SessionLimit { get; set; } = 0;
    public required int? SessionAvailabilityWarningThreshold { get; set; }
    public required int GamespaceLimitPerSession { get; set; } = 1;
    public required bool IsPublished { get; set; }
    public required bool AllowLateStart { get; set; }
    public required bool AllowPreview { get; set; }
    public required bool AllowPublicScoreboardAccess { get; set; }
    public required bool AllowReset { get; set; }
    public required string CardText1 { get; set; }
    public required string CardText2 { get; set; }
    public required string CardText3 { get; set; }
    public required bool IsFeatured { get; set; }
    public required string EngineMode { get; set; }
    public required PlayerMode PlayerMode { get; set; }

    // aggregates
    public int RegisteredTeamCount { get; set; }
    public int RegisteredUserCount { get; set; }
}

public sealed class ListGameResponseGameRegistration
{
    public required DateTimeOffset? EndTime { get; set; }
    public required DateTimeOffset? StartTime { get; set; }
    public required string RegistrationConstraint { get; set; }
    public required GameRegistrationType RegistrationType { get; set; }
}
