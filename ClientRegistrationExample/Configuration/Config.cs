namespace ClientRegistrationExample.Configuration;

internal class Config
{
    public required string Authority { get; set; }
    public required string ConfirmationUri { get; set; }
    public required string ClientDraftUri { get; set; }
    public required string ClientDraftApiKeyHeader { get; set; }
    public required string ClientDraftApiKey { get; set; }
    public required string ClientStatusUri { get; set; }
    public required string ClientSecretUri { get; set; }
    public required string RedirectPath { get; set; }
    public required int RedirectPort { get; set; }

    public required string OrganizationNumber { get; set; }
    public required string[] ApiScopes { get; set; }

    public required string HtmlTitle { get; set; }
    public required string HtmlBody { get; set; }

    public KeyValuePair<string, string>[]? AudienceSpecificClientClaims { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? ChildOrganizationNumbers { get; set; }
}