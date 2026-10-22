namespace Common.Models.Response;

public class ConformanceTestSession
{
    public required string SessionId { get; init; }

    /// <summary>
    /// Use this as the HelseID authority in your client for the duration of the session
    /// </summary>
    public required string AuthorityUri { get; init; }

    public required string ClientId { get; init; }
    public required ConformanceTestFlow Flow { get; init; }

    /// <summary>
    /// "active" or "ended". Kept as a string so that values added to the API later do not break deserialization.
    /// </summary>
    public required string Status { get; init; }

    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// Set when the session has ended
    /// </summary>
    public DateTime? EndedAt { get; init; }

    /// <summary>
    /// Set when the session has ended
    /// </summary>
    public ConformanceSessionResult? Result { get; init; }
}
