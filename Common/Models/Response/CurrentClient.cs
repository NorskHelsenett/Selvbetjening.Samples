namespace Common.Models.Response;

/// <summary>
/// Access to an API scope
/// </summary>
public class ApiScopeAccess
{
    /// <summary>
    /// Scope as defined by the API
    /// </summary>
    public required string Scope { get; set; }

    /// <summary>
    /// <see cref="ScopeAccessStatus"/>
    /// </summary>
    public required ScopeAccessStatus Status { get; set; }

    /// <summary>
    /// Human-readable reason for current status
    /// </summary>
    public required string? StatusReason { get; set; }
}

public class AudienceSpecificClientClaimAccess
{
    /// <summary>
    /// Claim type as defined by the API
    /// </summary>
    public required string ClaimType { get; set; }

    /// <summary>
    /// Claim value that will be included in access tokens for the API.
    /// Available if status is not 'pending'.
    /// </summary>
    public required string? ActiveClaimValue { get; set; }

    /// <summary>
    /// Pending claim value if status is 'pending' or 'updatePending'.
    /// The value is not included in access tokens until it has been approved.
    /// </summary>
    public required string? PendingClaimValue { get; set; }

    public required AudienceSpecificClaimStatus Status { get; set; }

    /// <summary>
    /// Human-readable reason for current status
    /// </summary>
    public required string? StatusReason { get; set; }
}

/// <summary>
/// Status of the access to an API scope:
/// <para>- ok - Scope can be used</para>
/// <para>- pending - Scope can not yet be used. See details in 'statusReason'.</para>
/// </summary>
public enum ScopeAccessStatus
{
    Ok,
    Pending,
}

/// <summary>
/// Status of audience specific client claim:
/// <para>- ok - Claim value from 'activeClaimValue' will be included in access tokens</para>
/// <para>- pending - Claim is pending and will not yet be included in access tokens. Pending value is available in 'pendingClaimValue'. See details in 'statusReason'.</para>
/// <para>- updatePending - Updated claim value is pending. Access tokens will include the claim value from 'activeClaimValue'. Pending value is available in 'pendingClaimValue'. See details in 'statusReason'.</para>
/// </summary>
public enum AudienceSpecificClaimStatus
{
    Ok,
    Pending,
    UpdatePending,
}

/// <summary>
/// Config for the current HelseID client.
/// </summary>
public class CurrentClient
{
    /// <summary>
    /// API scopes that this client has access to or has requested access to
    /// </summary>
    public ApiScopeAccess[] ApiScopes { get; set; } = [];

    /// <summary>
    /// Audience specific client claims set for this client, including pending claims
    /// </summary>
    public AudienceSpecificClientClaimAccess[] AudienceSpecificClientClaims { get; set; } = [];

    /// <summary>
    /// Redirect URIs that the client is allowed use with the authorization code flow
    /// </summary>
    public string[] RedirectUris { get; set; } = [];

    /// <summary>
    /// Post logout redirect URIs that the client is allowed to use for logging out the user.
    /// </summary>
    public string[] PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Which child organization numbers the client should be allowed to specify for an authorization request.
    /// </summary>
    public string[] ChildOrganizationNumbers { get; set; } = [];

    public ClientUpdate ToClientUpdate()
    {
        return new ClientUpdate
        {
            ApiScopes = ApiScopes.Select(s => s.Scope).ToArray(),
            AudienceSpecificClientClaims =
                AudienceSpecificClientClaims.Select(
                    a => new AudienceSpecificClientClaim(
                        a.ClaimType,
                        a.PendingClaimValue ?? a.ActiveClaimValue ??
                        throw new Exception($"Claim '{a.ClaimType}' has no value")
                    )
                ).ToArray(),
            RedirectUris = RedirectUris,
            ChildOrganizationNumbers = ChildOrganizationNumbers,
            PostLogoutRedirectUris = PostLogoutRedirectUris,
        };
    }
}
