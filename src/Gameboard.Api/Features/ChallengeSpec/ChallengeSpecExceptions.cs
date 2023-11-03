using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeSpecs;

public sealed class NonExistentSupportKey : GameboardValidationException
{
    public NonExistentSupportKey(string key) : base($"""No challenge spec exists with support key "{key}".""") { }
}
