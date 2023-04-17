namespace ClientRegistrationExample.Configuration;

internal class Config
{
    public required string Authority { get; set; }
    public required string ConfirmationUri { get; set; }
    public required string ClientDraftUri { get; set; }
    public required string ClientDraftApiKeyHeader { get; set; }
    public required string ClientDraftApiKey { get; set; }
    public required string ClientStatusUri { get; set; }
    public required string RedirectPath { get; set; }
    public required int RedirectPort { get; set; }
            
    public required string OrganizationNumber { get; set; }
    public required string[] ApiScopes { get; set; }

    public KeyValuePair<string, string>[]? AudienceSpecificClientClaims { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? ChildOrganizationNumbers { get; set; }

    public string HtmlTitle = "Client Registration Example";
    public string HtmlBody = "<h1>Du kan returnere til fagapplikasjonen din nå.</h1><p>Ikke lukk nettleseren hvis du ønsker å gjenbruke HelseID-påloggingen din, hvis mulig.</p>";
}