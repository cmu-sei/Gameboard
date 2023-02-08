namespace Gameboard.Api.Features.ApiKeys;

internal class InvalidApiKey : GameboardException
{
    public InvalidApiKey(string headerValue) : base("Your API key is invalid") { }
}

internal class InvalidApiKeyFormat : GameboardException
{
    public InvalidApiKeyFormat(string headerValue) : base($"Your API key is formatted incorrectly. Verify that you're sending the correct value or ask an admin to generate a new API key for this user account.") { }
}

