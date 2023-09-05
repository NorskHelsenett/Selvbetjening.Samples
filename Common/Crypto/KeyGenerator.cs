using Common.Extensions;
using Common.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Common.Crypto;

public static class KeyGenerator
{
    public static KeyPair GenerateRsa(KeySize keySize = KeySize.Bits2048)
    {
        using var rsa = RSA.Create(ToInt(keySize));
        return new KeyPair(rsa.ExportRSAPrivateKeyPem(), rsa.ExportRSAPublicKeyPem());
    }

    public static JwkWithMetadata GenerateRsaJwk(KeySize keySize = KeySize.Bits2048)
    {
        using var rsa = RSA.Create(ToInt(keySize));
        var rsaWithPrivateKey = new RsaSecurityKey(rsa.ExportParameters(true));
        var rsaWithoutPrivateKey = new RsaSecurityKey(rsa.ExportParameters(false));
        var jwkWithPrivateKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaWithPrivateKey);
        var jwkWithoutPrivateKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaWithoutPrivateKey);

        jwkWithPrivateKey.Kid = Base64UrlEncoder.Encode(jwkWithPrivateKey.ComputeJwkThumbprint());
        jwkWithoutPrivateKey.Kid = Base64UrlEncoder.Encode(jwkWithoutPrivateKey.ComputeJwkThumbprint());
        const string alg = SecurityAlgorithms.RsaSha512;
        jwkWithPrivateKey.Alg = alg;
        jwkWithoutPrivateKey.Alg = alg;

        string serializedJwkWithPrivateKey = Serialize(jwkWithPrivateKey, false);
        string serializedJwkWithoutPrivateKey = Serialize(jwkWithoutPrivateKey, false);

        return new (serializedJwkWithPrivateKey, serializedJwkWithoutPrivateKey, alg);
    }

    private static string Serialize(JsonWebKey jwk, bool indented = false)
    {
        var jwkfs = new JwkForSerialization
        {
            Alg = jwk.Alg,
            Crv = jwk.Crv,
            D = jwk.D,
            DP = jwk.DP,
            DQ = jwk.DQ,
            E = jwk.E,
            K = jwk.K,
            KeyOps = jwk.KeyOps?.Count > 0 ? jwk.KeyOps : null,
            Kid = jwk.Kid,
            Kty = jwk.Kty,
            N = jwk.N,
            Oth = jwk.Oth,
            P = jwk.P,
            Q = jwk.Q,
            QI = jwk.QI,
            Use = jwk.Use,
            X = jwk.X,
            Y = jwk.Y,
            X5c = jwk.X5c?.Count > 0 ? jwk.X5c : null,
            X5t = jwk.X5t,
            X5u = jwk.X5u,
            X5tS256 = jwk.X5tS256
        };

        return jwkfs.ToJson(indented);
    }

    private static int ToInt(KeySize keySize)
    {
        return keySize switch
        {
            KeySize.Bits2048 => 2048,
            KeySize.Bits4096 => 4096,
            _ => throw new Exception($"Unhandled key size: {keySize}"),
        };
    }
}

public enum KeySize
{
    Bits2048, Bits4096
}