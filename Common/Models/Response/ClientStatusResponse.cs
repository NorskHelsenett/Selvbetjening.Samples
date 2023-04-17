namespace Common.Models.Response;

public class ClientStatusResponse
{
    public OverallClientStatus Status { get; set; }

    public required Details Details { get; set; }
}

public class ApiScopeAccessStatus
{
    public bool Granted { get; set; }
    public required string Scope { get; set; }
    public ApiScopeAccessReason? Reason { get; set; }
}

public enum ApiScopeAccessReason
{
    ApiAccessPending,
    ApiAccessRejected,
    ApiAccessRevoked,
    ApiScopeAccessPending,
    ApiScopeAccessRejected,
    ApiScopeAccessRevoked,
}

public enum OverallClientStatus
{
    Active,
    Limited,
    Blocked
}

public class Details
{
    public required ClientActiveStatus ClientStatus { get; set; }

    public required List<ApiScopeAccessStatus> ApiScopeAccessStatuses { get; set; }
}

public class ClientActiveStatus
{
    public bool Active { get; set; }
    public ClientActiveStatusReason? Reason { get; set; }
}

public enum ClientActiveStatusReason
{
    AwaitingOwnerConfirmation,
    BlockedByAdmin,
}