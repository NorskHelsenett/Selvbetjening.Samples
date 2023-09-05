using Microsoft.IdentityModel.Tokens;

namespace Common.Models;

public class JwkWithMetadata
{
    public string Algorithm { get; }
    public string PublicAndPrivateValue { get; }
    public string PublicValue { get; }

    public JwkWithMetadata(string publicAndPrivateValue, string publicValue, string defaultAlgorithm)
    {
        PublicAndPrivateValue = publicAndPrivateValue;
        PublicValue = publicValue;

        Algorithm = new JsonWebKey(publicAndPrivateValue).Alg;
        if (string.IsNullOrEmpty(Algorithm))
        {
            Algorithm = defaultAlgorithm;
        }
    }
}