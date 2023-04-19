namespace ClientRegistrationExample.Configuration;

internal class Config
{
    public required HelseIdConfig HelseId { get; set; }
    public required SelvbetjeningConfig Selvbetjening { get; set; }
    public required ClientDraftConfig ClientDraft { get; set; }
    public required LocalHttpServerConfig LocalHttpServer { get; set; }
}

internal class HelseIdConfig
{
    public required string Authority { get; set; }
}

internal class SelvbetjeningConfig
{
    public required string ConfirmationUri { get; set; }
    public required string ApiUri { get; set; }
    public required string ClientDraftEndpoint { get; set; }
    public required string ClientStatusEndpoint { get; set; }
    public required string ClientSecretEndpoint { get; set; }
    public required string ClientDraftApiKeyHeader { get; set; }
    public required string ClientDraftApiKey { get; set; }

    public string ClientDraftUri => $"{ApiUri}/{ClientDraftEndpoint}";
    public string ClientStatusUri => $"{ApiUri}/{ClientStatusEndpoint}";
    public string ClientSecretUri => $"{ApiUri}/{ClientSecretEndpoint}";
}

internal class ClientDraftConfig
{
    public required string OrganizationNumber { get; set; }
    public required string[] ApiScopes { get; set; }
    public KeyValuePair<string, string>[]? AudienceSpecificClientClaims { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? ChildOrganizationNumbers { get; set; }
}

internal class LocalHttpServerConfig
{
    public required string RedirectPath { get; set; }
    public required int RedirectPort { get; set; }
    public required string HtmlTitle { get; set; }
    public required string HtmlBody { get; set; }
}