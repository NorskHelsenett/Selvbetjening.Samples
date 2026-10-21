namespace Common.Models.Response;

public class ConformanceSessionResult
{
    /// <summary>Whether the client passed. A warning also means it did not pass.</summary>
    public required bool Passed { get; set; }

    /// <summary>The requests the client made to the test STS, with the checks run on each.</summary>
    public ConformanceRequest[] Requests { get; set; } = [];
}

public class ConformanceRequest
{
    /// <summary>Which STS endpoint was called, for example "Metadata" or "Token".</summary>
    public required string EndpointName { get; set; }

    /// <summary>Only present for token requests.</summary>
    public ConformanceTokenRequestDetails? TokenRequest { get; set; }

    public ConformanceCheck[] Checks { get; set; } = [];
}

public class ConformanceTokenRequestDetails
{
    public required string GrantType { get; set; }

    /// <summary>The resource parameter, if one was sent.</summary>
    public string? ResourceIndicator { get; set; }

    /// <summary>The first DPoP token request normally has no nonce.</summary>
    public bool DpopNonceIncluded { get; set; }
}

public class ConformanceCheck
{
    /// <summary>
    /// Identifies the check, for example "TokenCorrectScopes". New values may be added, so treat
    /// unrecognised ones as opaque.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// "passed", "warning" or "failed". Read as text rather than an enum so that a value this
    /// sample does not know about is printed instead of failing to deserialize.
    /// </summary>
    public required string Outcome { get; set; }

    public required LocalizedText Title { get; set; }

    /// <summary>Why the check failed or warned. Empty when it passed.</summary>
    public LocalizedText[] Errors { get; set; } = [];

    public bool Passed => string.Equals(Outcome, "passed", StringComparison.OrdinalIgnoreCase);
}

public class LocalizedText
{
    public required string En { get; set; }
    public required string Nb { get; set; }
}
