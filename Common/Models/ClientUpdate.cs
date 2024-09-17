namespace Common.Models
{
    public class ClientUpdate
    {
        public required string[] ApiScopes { get; set; } = [];
        public required AudienceSpecificClientClaim[]? AudienceSpecificClientClaims { get; set; }
        public required string[]? RedirectUris { get; set; }
        public required string[]? PostLogoutRedirectUris { get; set; }
        public required string[]? ChildOrganizationNumbers { get; set; }
    }
}