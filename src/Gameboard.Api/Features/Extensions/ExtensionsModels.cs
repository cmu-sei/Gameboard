using System;
using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Extensions;

public sealed class GbExtension
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ExtensionType Type { get; set; }
    public required string HostUrl { get; set; }
}

public sealed class ExtensionsTeamScoredEvent
{
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Team { get; set; }
    public required ExtensionsTeamScoredEventScore ScoringSummary { get; set; }
    public required int Rank { get; set; }
}

public sealed class ExtensionsTeamScoredEventScore
{
    public string ChallengeName { get; set; }
    public bool IsChallengeManualBonus { get; set; }
    public bool IsTeamManualBonus { get; set; }
    public double Points { get; set; }
}

public sealed class ExtensionMessage
{
    public required string Text { get; set; }
    public IEnumerable<KeyValuePair<string, string>> TextAttachments { get; set; } = Array.Empty<KeyValuePair<string, string>>();
}

public sealed class ExtensionNotificationException : GameboardException
{
    public ExtensionNotificationException(string id, ExtensionType type, Exception innerEx)
        : base($"{type.ToString()} extension {id} failed during a notification.", innerEx) { }
}
