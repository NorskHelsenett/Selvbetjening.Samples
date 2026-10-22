namespace Common.Models;

/// <summary>
/// Which client to run the conformance test with, and over which authentication flow. The client
/// must belong to the client system the API key was created for.
/// </summary>
/// <param name="Flow">
/// Authentication flow to test. If omitted, clients supporting user login default to userLogin,
/// and clients only supporting API calls default to machineToMachine.
/// </param>
public record ConformanceSessionStartRequest(string ClientId, ConformanceTestFlow? Flow = null);
