using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gameboard.Api.Structure.Auth.Crucible;

public static partial class CrucibleIPrincipalExtensions
{
    /// <summary>
    /// Given a principal and a JSON path to one of its properties (because all of our current principals 
    /// are built from JWTs), return all role claims present. Values which are not arrays in the principal will be
    /// array-ified.
    /// </summary>
    /// <param name="principal">A principal with 0 or more claims.</param>
    /// <param name="roleClaimPath">A path to a property of the principal</param>
    /// <returns>An array of zero or more role claims at the specified path.</returns>
    public static string[] GetRoleClaims(this ClaimsPrincipal principal, string roleClaimPath)
    {
        // If the claim path is a level or more deep (e.g. "realm_access.roles", as it is in a KeyCloak JWT),
        // there will be a claim with type equal to the first part of the path, so isolate it
        return GetClaimsFromToken(principal, roleClaimPath);
    }

    private static string[] GetClaimsFromToken(ClaimsPrincipal principal, string claimPath)
    {
        if (string.IsNullOrEmpty(claimPath))
        {
            return [];
        }

        // Name of the claim to insert into the token. This can be a fully qualified name like 'address.street'.
        // In this case, a nested json object will be created. To prevent nesting and use dot literally, escape the dot with backslash (\.).
        var pathSegments = NonEscapedDotRegex().Split(claimPath).Select(s => s.Replace("\\.", ".")).ToArray();
        var tokenClaim = principal.Claims.Where(x => x.Type == pathSegments.First()).FirstOrDefault();

        return tokenClaim?.ValueType switch
        {
            ClaimValueTypes.String => [tokenClaim.Value],
            JsonClaimValueTypes.Json => ExtractJsonClaimValues(tokenClaim.Value, pathSegments.Skip(1)),
            _ => []
        };
    }

    private static string[] ExtractJsonClaimValues(string json, IEnumerable<string> pathSegments)
    {
        var values = new List<string>();
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement currentElement = doc.RootElement;

            foreach (var segment in pathSegments)
            {
                if (!currentElement.TryGetProperty(segment, out JsonElement propertyElement))
                {
                    return [];
                }

                currentElement = propertyElement;
            }

            if (currentElement.ValueKind == JsonValueKind.Array)
            {
                values.AddRange(currentElement.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()));
            }
            else if (currentElement.ValueKind == JsonValueKind.String)
            {
                values.Add(currentElement.GetString());
            }
        }
        catch (JsonException)
        {
            // Handle invalid JSON format
        }

        return [.. values];
    }

    [GeneratedRegex(@"(?<!\\)\.")]
    private static partial Regex NonEscapedDotRegex();
}
