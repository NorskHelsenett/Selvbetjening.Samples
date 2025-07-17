using Common.Models;

namespace ClientRegistrationExample.Configuration;

internal class Config
{
    public required HelseIdConfig HelseId { get; init; }
    public required SelvbetjeningConfig Selvbetjening { get; init; }
    public required ClientDraftConfig ClientDraft { get; init; }
    public required LocalHttpServerConfig LocalHttpServer { get; init; }
    public required AppConfig App { get; init; }
}

internal class HelseIdConfig
{
    public required string Authority { get; set; }
    public required bool UseDPoP { get; set; }
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

    public string ClientDraftUri => GetEndpointUri(ClientDraftEndpoint);
    public string ClientStatusUri => GetEndpointUri(ClientStatusEndpoint);
    public string ClientSecretUri => GetEndpointUri(ClientSecretEndpoint);

    private string GetEndpointUri(string endpointPath) => new Uri(new Uri(ApiUri), endpointPath).ToString();
}

internal class ClientDraftConfig
{
    public required string OrganizationNumber { get; set; }
    public required string[] ApiScopes { get; set; }
    public AudienceSpecificClientClaim[]? AudienceSpecificClientClaims { get; set; }
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

internal class AppConfig
{
    public required bool UserLogin { get; set; }
}
