using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Support;

public sealed class SupportSettingsViewModel
{
    public required SupportSettingsAutoTag[] AutoTags { get; set; }
    public string AutoTagPracticeTicketsWith { get; set; }
    public string SupportPageGreeting { get; set; }
}
