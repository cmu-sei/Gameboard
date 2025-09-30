// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Challenges;

public sealed class ChallengeSubmissionViewModel
{
    public required int SectionIndex { get; set; }
    public required IEnumerable<string> Answers { get; set; }
    public required DateTimeOffset SubmittedOn { get; set; }
}
