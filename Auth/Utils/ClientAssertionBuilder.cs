using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Auth.Utils;

public static class ClientAssertionBuilder
{
    public static ClientAssertion GetClientAssertion(string clientId, string jwk, string authority, string? orgNo = null)
    {
        var clientAssertionString = BuildClientAssertion(clientId, jwk, authority, orgNo);

        return new ClientAssertion
        {
            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
            Value = clientAssertionString
        };
    }

    private static string BuildClientAssertion(string clientId, string jwk, string authority, string? orgNo)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Subject, clientId),
            new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
            new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
        };

        if (orgNo != null)
        {
            claims.Add(new Claim("authorization_details",
@"{
    ""type"":""helseid_authorization"",
    ""practitioner_role"":
    {
        ""organization"":
        {
            ""identifier"":
            {
                ""system"":""urn:oid:1.0.6523"",
                ""type"":""ENH"",
                ""value"":""NO:ORGNR:<consumer organization number>"",
            }
        }
    }
}".Replace("\r\n", string.Empty).Replace("<consumer organization number>", orgNo), JsonClaimValueTypes.Json));
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