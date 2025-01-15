using IdentityModel;
using IdentityModel.Client;

namespace Auth.Utils;

public static class ClientAssertionBuilder
{
    public static ClientAssertion Build(string clientId, string jwk, string authority, string[]? assertionDetails = null)
    {
        var clientAssertionString = HelseIdJwtBuilder.Build(clientId, jwk, authority, assertionDetails: assertionDetails);

        return new ClientAssertion
        {
            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
            Value = clientAssertionString
        };
    }
}
