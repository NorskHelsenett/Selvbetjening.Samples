namespace Common.Models.Response;

public class ConformanceSessionStartResponse
{
    public required string SessionId { get; set; }

    /// <summary>
    /// Configure this as the HelseID 'authority' in your application for the duration of the test.
    /// </summary>
    public required string AuthorityUri { get; set; }
}
