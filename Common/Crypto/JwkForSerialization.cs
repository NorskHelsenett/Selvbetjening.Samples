using System.Text.Json.Serialization;

namespace Common.Crypto;

/// <summary>
/// https://www.rfc-editor.org/rfc/rfc7517
/// </summary>
internal class JwkForSerialization
{
    /// <summary>'alg' (KeyType)</summary>
    [JsonPropertyName("alg")]
    public string? Alg { get; internal set; }

    /// <summary>'crv' (ECC - Curve)</summary>
    [JsonPropertyName("crv")]
    public string? Crv { get; internal set; }

    /// <summary>'d' (ECC - Private Key OR RSA - Private Exponent)</summary>
    [JsonPropertyName("d")]
    public string? D { get; internal set; }

    /// <summary>'dp' (RSA - First Factor CRT Exponent)</summary>
    [JsonPropertyName("dp")]
    public string? Dp { get; internal set; }

    /// <summary>'dq' (RSA - Second Factor CRT Exponent)</summary>
    [JsonPropertyName("dq")]
    public string? Dq { get; internal set; }

    /// <summary>'e' (RSA - Exponent)</summary>
    [JsonPropertyName("e")]
    public string? E { get; internal set; }

    /// <summary>'k' (Symmetric - Key Value)</summary>
    [JsonPropertyName("k")]
    public string? K { get; internal set; }

    /// <summary>'key_ops' (Key Operations)</summary>
    [JsonPropertyName("key_ops")]
    public IList<string>? KeyOps { get; internal set; }

    /// <summary>'kid' (Key ID)</summary>
    [JsonPropertyName("kid")]
    public string? Kid { get; internal set; }

    /// <summary>'kty' (Key Type)</summary>
    [JsonPropertyName("kty")]
    public string? Kty { get; internal set; }

    /// <summary>'n' (RSA - Modulus)</summary>
    [JsonPropertyName("n")]
    public string? N { get; internal set; }

    /// <summary>'oth' (RSA - Other Primes Info)</summary>
    [JsonPropertyName("oth")]
    public IList<string>? Oth { get; internal set; }

    /// <summary>'p' (RSA - First Prime Factor)</summary>
    [JsonPropertyName("p")]
    public string? P { get; internal set; }

    /// <summary>'q' (RSA - Second Prime Factor)</summary>
    [JsonPropertyName("q")]
    public string? Q { get; internal set; }

    /// <summary>'qi' (RSA - First CRT Coefficient)</summary>
    [JsonPropertyName("qi")]
    public string? Qi { get; internal set; }

    /// <summary>'use' (Public Key Use)</summary>
    [JsonPropertyName("use")]
    public string? Use { get; internal set; }

    /// <summary>'x' (ECC - X Coordinate)</summary>
    [JsonPropertyName("x")]
    public string? X { get; internal set; }

    /// <summary>'x5c' collection (X.509 Certificate Chain)</summary>
    [JsonPropertyName("x5c")]
    public IList<string>? X5C { get; internal set; }

    /// <summary>'x5t' (X.509 Certificate SHA-1 thumbprint)</summary>
    [JsonPropertyName("x5t")]
    public string? X5T { get; internal set; }

    /// <summary>'x5t#S256' (X.509 Certificate SHA-256 thumbprint)</summary>
    [JsonPropertyName("x5t#S256")]
    public string? X5Ts256 { get; internal set; }

    /// <summary>'x5u' (X.509 URL)</summary>
    [JsonPropertyName("x5u")]
    public string? X5U { get; internal set; }

    /// <summary>'y' (ECC - Y Coordinate)</summary>
    [JsonPropertyName("y")]
    public string? Y { get; internal set; }
}