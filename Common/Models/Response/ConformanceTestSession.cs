using System.Text.Json.Serialization;
using Takt.Models.Responses.ExternalApi;

namespace Common.Models.Response;

public class ConformanceTestSession
{
    /// <summary>
    /// ID used to identify the test session
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// URI to configure as the HelseID 'authority' in your application (replacing 'https://helseid-sts.test.nhn.no/')
    /// </summary>
    public required string AuthorityUri { get; init; }

    /// <summary>
    /// HelseID client id of the tested client
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientId { get; init; }

    /// <summary>
    /// Authentication flow being tested
    /// </summary>
    public required SamsvarstestingFlow Flow { get; init; }

    /// <summary>
    /// Status of the session. NOTE: New values may be added; treat unknown values as opaque.
    /// </summary>
    public required ConformanceTestSessionStatus Status { get; init; }

    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// Set when the session has ended
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? EndedAt { get; init; }

    /// <summary>
    /// Set when the session has ended
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConformanceSessionResult? Result { get; init; }
}

public enum ConformanceTestSessionStatus
{
    /// <summary>
    /// The client can be tested against the session's authority
    /// </summary>
    Active,

    /// <summary>
    /// The session has ended and has results
    /// </summary>
    Ended,
}
