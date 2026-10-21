using System.Text.Json.Serialization;
using Takt.Models.Responses.ExternalApi;

namespace Common.Models.Response;

public class ConformanceSessionResult
{
    /// <summary>
    /// Whether the client passed the conformance test. False if any check failed or has a warning.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Requests the client made to the test STS during the session, with the checks performed on each
    /// </summary>
    public required ConformanceRequest[] Requests { get; init; }
}

public class ConformanceRequest
{
    /// <summary>
    /// Name of the STS endpoint that was called, e.g. "Metadata", "PAR" or "Token".
    ///
    /// New values may be added; treat unknown values as opaque.
    /// </summary>
    public required string EndpointName { get; init; }

    /// <summary>
    /// Details that distinguish token requests from each other. Only present for token requests.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConformanceTokenRequestDetails? TokenRequest { get; init; }

    /// <summary>
    /// Checks performed on the request
    /// </summary>
    public required ConformanceCheck[] Checks { get; init; }
}

public class ConformanceTokenRequestDetails
{
    /// <summary>
    /// The grant_type parameter of the token request, e.g. "authorization_code" or "refresh_token"
    /// </summary>
    public required string GrantType { get; init; }

    /// <summary>
    /// The resource parameter (RFC 8707) of the token request. Only present if it was sent.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceIndicator { get; init; }

    /// <summary>
    /// Whether the request's DPoP proof included a nonce. False if the proof was missing or could not be read.
    ///
    /// The first DPoP token request normally has no nonce. The proof's validity is reported by the checks, not by this field.
    /// </summary>
    public required bool DpopNonceIncluded { get; init; }
}

public class ConformanceCheck
{
    /// <summary>
    /// Code identifying the check, e.g. "TokenCorrectScopes". The same code appears once per request it applies to.
    ///
    /// New values may be added; treat unknown values as opaque. "Unknown" is used for checks this API does not
    /// recognise yet, and is not a stable identifier.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Outcome of the check. A warning also makes the session not pass; see <see cref="ConformanceSessionResult.Passed"/>.
    /// </summary>
    public required ConformanceCheckOutcome Outcome { get; init; }

    /// <summary>
    /// Human-readable description of the check
    /// </summary>
    public required LocalizedText Title { get; init; }

    /// <summary>
    /// Why the check failed or has a warning. Empty if it passed.
    /// </summary>
    public required LocalizedText[] Errors { get; init; }
}

public enum ConformanceCheckOutcome
{
    /// <summary>
    /// The check passed
    /// </summary>
    Passed,

    /// <summary>
    /// The check found an issue that should be fixed
    /// </summary>
    Warning,

    /// <summary>
    /// The check failed
    /// </summary>
    Failed,
}
