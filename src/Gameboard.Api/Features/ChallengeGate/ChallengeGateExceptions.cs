using Gameboard.Api;

internal class CyclicalGateConfiguration : GameboardException
{
    public CyclicalGateConfiguration(string targetId, string requiredId, string cycleDesc)
    : base($"Challenge spec {requiredId} can't be used as a prerequisite for challenge spec {targetId} because they have cyclical dependency ({cycleDesc}).") { }
}
