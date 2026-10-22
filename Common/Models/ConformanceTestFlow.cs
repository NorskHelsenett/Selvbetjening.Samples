namespace Common.Models;

public enum ConformanceTestFlow
{
    /// <summary>
    /// Only machine-to-machine API calls, no user login (i.e. client_credentials grant).
    /// Requires the client to have API access enabled.
    /// </summary>
    MachineToMachine,

    /// <summary>
    /// User login, optionally including API calls (i.e. authorization_code grant).
    /// Requires the client to have user login enabled.
    /// </summary>
    UserLogin
}
