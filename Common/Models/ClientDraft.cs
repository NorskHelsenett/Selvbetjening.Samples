namespace Common.Models;

public class ClientDraft
{
    public ClientDraft(string organizationNumber, string publicJwk, string[] apiScopes, string postClientConfirmationRedirectUri)
    {
        OrganizationNumber = organizationNumber;
        PublicJwk = publicJwk;
        ApiScopes = apiScopes;
        PostClientConfirmationRedirectUri = postClientConfirmationRedirectUri;
    }

    public string OrganizationNumber { get; set; }
    public string PublicJwk { get; set; }
    public string[] ApiScopes { get; set; }

    public AudienceSpecificClientClaim[]? AudienceSpecificClientClaims { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? ChildOrganizationNumbers { get; set; }

    public string PostClientConfirmationRedirectUri { get; set; }
}