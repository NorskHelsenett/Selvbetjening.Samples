using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Auth.Utils;

public static class HelseIdJwtBuilder
{
    public static string Build(string clientId, string jwk, string authority, string[]? assertionDetails = null, string[]? authorizationDetails = null)
    {
        bool hasAssertionDetails = assertionDetails != null && assertionDetails.Length > 0;
        bool hasAuthorizationDetails = authorizationDetails != null && authorizationDetails.Length > 0;

        if (hasAssertionDetails && hasAuthorizationDetails)
        {
            throw new Exception("Authorization details are for the request object and assertion details are for the client assertion. Both should not be set at the same time.");
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, clientId),
            new(JwtClaimTypes.IssuedAt, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
        };

        if (hasAuthorizationDetails)
        {
            claims.Add(CreateDetailsClaim("authorization_details", authorizationDetails!));
        }
        else if (hasAssertionDetails)
        {
            claims.Add(CreateDetailsClaim("assertion_details", assertionDetails!));
        }

        var credentials =
            new JwtSecurityToken(
                clientId,
                authority,
                claims,
                DateTime.UtcNow,
                DateTime.UtcNow.AddSeconds(60),
                GetClientAssertionSigningCredentials(jwk));

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(credentials);
    }

    private static Claim CreateDetailsClaim(string claimType, string[] details)
    {
        return details.Length == 1
            ? new Claim(claimType, details.Single(), JsonClaimValueTypes.Json)
            : new Claim(claimType, $"[{string.Join(",", details)}]", JsonClaimValueTypes.JsonArray);
    }

    private static SigningCredentials GetClientAssertionSigningCredentials(string jwk)
    {
        var securityKey = new JsonWebKey(jwk);

        var alg = securityKey.Alg;
        if (string.IsNullOrWhiteSpace(alg))
        {
            throw new Exception("JWK must include the 'alg' parameter");
        }

        return new SigningCredentials(securityKey, alg);
    }
}