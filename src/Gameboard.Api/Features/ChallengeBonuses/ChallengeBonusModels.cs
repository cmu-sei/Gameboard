using System;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.ChallengeBonuses;

public class CreateManualChallengeBonus
{
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public class UpdateManualChallengeBonus
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public class ManualChallengeBonusViewModel
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
    public DateTimeOffset EnteredOn { get; set; }
    public SimpleEntity EnteredBy { get; set; }
    public string ChallengeId { get; set; }
}
