namespace Common.Models
{
    public class ClientUpdate
    {
        public string[] ApiScopes { get; set; } = Array.Empty<string>();
        public AudienceSpecificClientClaim[]? AudienceSpecificClientClaims { get; set; }
        public string[]? RedirectUris { get; set; }
        public string[]? PostLogoutRedirectUris { get; set; }
        public string[]? ChildOrganizationNumbers { get; set; }
    }
}