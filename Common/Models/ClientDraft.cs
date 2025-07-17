namespace Common.Models;

public class ClientDraft(
    string organizationNumber,
    string publicJwk,
    string[] apiScopes,
    string postClientConfirmationRedirectUri)
{
    public string OrganizationNumber { get; set; } = organizationNumber;
    public string PublicJwk { get; set; } = publicJwk;
    public string[] ApiScopes { get; set; } = apiScopes;

    public AudienceSpecificClientClaim[]? AudienceSpecificClientClaims { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? ChildOrganizationNumbers { get; set; }

    public string PostClientConfirmationRedirectUri { get; set; } = postClientConfirmationRedirectUri;
}