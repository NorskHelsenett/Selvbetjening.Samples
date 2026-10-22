namespace ConformanceTestExample.Configuration;

internal class Config
{
    public required HelseIdConfig HelseId { get; init; }
    public required SelvbetjeningConfig Selvbetjening { get; init; }
    public required ClientConfig Client { get; init; }
}

internal class HelseIdConfig
{
    public required bool UseDPoP { get; set; }
}

internal class SelvbetjeningConfig
{
    public required string ApiUri { get; set; }
    public required string ConformanceSessionEndpoint { get; set; }
    public required string ConformanceTestApiKeyHeader { get; set; }
    public required string ConformanceTestApiKey { get; set; }

    public string ConformanceSessionUri => new Uri(new Uri(ApiUri), ConformanceSessionEndpoint).ToString();

    public string SessionUri(string sessionId) => $"{ConformanceSessionUri}/{sessionId}";
    public string EndSessionUri(string sessionId) => $"{SessionUri(sessionId)}/end";
    public string ResetSessionUri(string sessionId) => $"{SessionUri(sessionId)}/reset";
}

/// <summary>
/// An existing HelseID client to run the test with. It has to belong to the same client system the
/// API key was created for, or starting a session is rejected.
/// </summary>
internal class ClientConfig
{
    public required string ClientId { get; set; }

    /// <summary>The client's private JWK, as JSON.</summary>
    public required string Jwk { get; set; }

    public required string[] Scopes { get; set; }
}
