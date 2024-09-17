namespace Common.Models;

public class ApiScopeAccess
{
    public required string Scope { get; set; }
    public required ScopeAccessStatus Status { get; set; }
    public required string? StatusReason { get; set; }
}

public class AudienceSpecificClientClaimAccess
{
    public required string ClaimType { get; set; }
    public required string? ActiveClaimValue { get; set; }
    public required string? PendingClaimValue { get; set; }
    public required AudienceSpecificClaimStatus Status { get; set; }
    public required string? StatusReason { get; set; }
}

public enum ScopeAccessStatus
{
    Ok,
    Pending,
}

public enum AudienceSpecificClaimStatus
{
    Ok,
    Pending,
    UpdatePending,
}

public class CurrentClient
{
    public ApiScopeAccess[] ApiScopes { get; set; } = [];
    public AudienceSpecificClientClaimAccess[] AudienceSpecificClientClaims { get; set; } = [];
    public string[] RedirectUris { get; set; } = [];
    public string[] PostLogoutRedirectUris { get; set; } = [];
    public string[] ChildOrganizationNumbers { get; set; } = [];
}