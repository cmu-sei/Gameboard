// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Support;

public sealed class SupportSettingsViewModel
{
    public required SupportSettingsAutoTag[] AutoTags { get; set; }
    public string SupportPageGreeting { get; set; }
}

public sealed class SupportSettingsAutoTagViewModel
{
    public required string Id { get; set; }
    public required SupportSettingsAutoTagConditionType ConditionType { get; set; }
    public required string ConditionTypeDescription { get; set; }
    public required string ConditionValue { get; set; }
    public required string Tag { get; set; }
}

public sealed class UpsertSupportSettingsAutoTagRequest
{
    public string Id { get; set; }
    public required SupportSettingsAutoTagConditionType ConditionType { get; set; }
    public required string ConditionValue { get; set; }
    public bool? IsEnabled { get; set; }
    public required string Tag { get; set; }
}

public static class TicketStatus
{
    public static readonly string Closed = "Closed";
    public static readonly string Open = "Open";
}
