using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Api.Structure.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    private readonly static int MIN_BYTES_RANDOMNESS = 16;
    private readonly static int MIN_RANDOMNESS_LENGTH = 10;

    public int BytesOfRandomness { get; set; } = 32;
    public string KeyPrefix { get; set; } = "GB";
    public int RandomCharactersLength { get; set; } = 36;

    public override void Validate()
    {
        if (BytesOfRandomness < MIN_BYTES_RANDOMNESS || RandomCharactersLength < MIN_RANDOMNESS_LENGTH)
        {
            throw new InvalidApiKeyAuthenticationOptions("Invalid configuration of API key authentication. The minimum value ");
        }
    }
}
