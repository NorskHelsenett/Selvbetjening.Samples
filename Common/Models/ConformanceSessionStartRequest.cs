namespace Common.Models;

/// <summary>
/// Which client to run the conformance test with, and over which authentication flow. The client
/// must belong to the client system the API key was created for.
/// </summary>
public class ConformanceSessionStartRequest(string clientId, string flow)
{
    public string ClientId { get; set; } = clientId;

    /// <summary>"machineToMachine" or "userLogin".</summary>
    public string Flow { get; set; } = flow;
}
