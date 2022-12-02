using System.Collections.Generic;

public class CubespaceScoreboardCodex
{
    public string Codename { get; set; }
    public long ScoredAt { get; set; }
}

public class CubespaceScoreboardSponsor
{
    public string Name { get; set; }
    public string LogoUri { get; set; }
}

public class CubespaceScoreboardPlayer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public CubespaceScoreboardSponsor Sponsor { get; set; }
}

public class CubespaceScoreboardTeam
{
    public string Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<CubespaceScoreboardPlayer> Players { get; set; } = new List<CubespaceScoreboardPlayer>();
    public long Day1Playtime { get; set; }
    public int Day1Score { get; set; }
    public int CubespaceScore { get; set; }
    public int Rank { get; set; }
    public IList<CubespaceScoreboardCodex> ScoredCodexes { get; set; } = new List<CubespaceScoreboardCodex>();
    // unix millis
    public long? CubespaceStartTime { get; set; }
}

public class CubespaceScoreboardState
{
    public string Day1GameId { get; set; }
    public string CubespaceGameId { get; set; }
    public long GameOverAt { get; set; }
    public IEnumerable<CubespaceScoreboardTeam> Teams { get; set; }
}

public static class CubespaceCodexName
{
    public static readonly string AncientRuins = "antruins";
    public static readonly string AurellianMuseum = "cllctn";
    public static readonly string Exoarchaeology = "exoarch";
    public static readonly string RedRaider = "redradr";
    public static readonly string Xenocult = "fllwrs";
    public static readonly string LagrangePoint = "yrmssn";
}

public class CubespaceScoreboardCache
{
    public long? GameOverAt { get; set; }
    public IEnumerable<CubespaceScoreboardSponsor> Sponsors { get; set; } = new List<CubespaceScoreboardSponsor>();
    public Dictionary<string, CubespaceScoreboardCacheTeam> Teams { get; } = new Dictionary<string, CubespaceScoreboardCacheTeam>();
}

public class CubespaceScoreboardCacheTeam
{
    public string Id { get; set; }
    public CubespaceScoreboardCacheChallenge CubespaceChallenge { get; set; }
    public CubespaceScoreboardCacheChallenge Day1Challenge { get; set; }
}

public class CubespaceScoreboardCacheChallenge
{
    public string Id { get; set; }
    public string GameId { get; set; }
    public string TeamId { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public int Score { get; set; }

    public long GetDuration()
    {
        return EndTime - StartTime;
    }
}

public class CubespaceScoreboardRequestPayload
{
    public string CubespaceGameId { get; set; }
    public string Day1GameId { get; set; }
}