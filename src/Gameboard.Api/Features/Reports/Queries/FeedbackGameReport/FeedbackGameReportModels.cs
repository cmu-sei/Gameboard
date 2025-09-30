// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api.Features.Reports;

public sealed class FeedbackGameReportResults
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<FeedbackQuestion> Questions { get; set; }
    public FeedbackStats Stats { get; set; }
}
