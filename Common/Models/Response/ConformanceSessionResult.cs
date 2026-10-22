namespace Common.Models.Response;

public class ConformanceSessionResult
{
    /// <summary>
    /// False if any check failed or has a warning
    /// </summary>
    public required bool Passed { get; init; }

    public required ConformanceRequest[] Requests { get; init; }
}

public class ConformanceRequest
{
    /// <summary>
    /// The endpoint the client called, e.g. "Metadata", "PAR" or "Token"
    /// </summary>
    public required string EndpointName { get; init; }

    /// <summary>
    /// Only set for token requests
    /// </summary>
    public ConformanceTokenRequestDetails? TokenRequest { get; init; }

    public required ConformanceCheck[] Checks { get; init; }
}

public class ConformanceTokenRequestDetails
{
    public required string GrantType { get; init; }
    public string? ResourceIndicator { get; init; }
    public required bool DpopNonceIncluded { get; init; }
}

public class ConformanceCheck
{
    public required string Code { get; init; }

    /// <summary>
    /// "passed", "warning" or "failed". Kept as a string so that values added to the API later do not break deserialization.
    /// </summary>
    public required string Outcome { get; init; }

    public required LocalizedText Title { get; init; }

    /// <summary>
    /// Why the check failed or has a warning. Empty if it passed.
    /// </summary>
    public required LocalizedText[] Errors { get; init; }
}
