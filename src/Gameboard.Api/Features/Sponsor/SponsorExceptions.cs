namespace Gameboard.Api.Features.Sponsors;

internal class CouldntResolveDefaultSponsor : GameboardException
{
    public CouldntResolveDefaultSponsor()
        : base($"Couldn't resolve a default sponsor. This Gameboard installation has no sponsors configured. At least one is required. Vist Admin -> Sponsors to add one.") { }
}
