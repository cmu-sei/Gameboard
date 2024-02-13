using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Sponsors;

internal class CantSetSponsorAsParentOfItself : GameboardValidationException
{
    public CantSetSponsorAsParentOfItself(string id)
        : base($"Sponsor {id} can't be set as parent of itself.") { }
}

internal class CouldntResolveDefaultSponsor : GameboardException
{
    public CouldntResolveDefaultSponsor()
        : base($"Couldn't resolve a default sponsor. This Gameboard installation has no sponsors configured. At least one is required. Vist Admin -> Sponsors to add one.") { }
}

internal class PlayerHasDefaultSponsor : GameboardValidationException
{
    public PlayerHasDefaultSponsor(string playerId)
        : base($"Player {playerId} hasn't selected a sponsor. Players must select a sponsor in their profile before joining a game.") { }

}
