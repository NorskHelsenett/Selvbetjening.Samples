namespace ClientUpdateExample.Configuration;

internal class Config
{
    public required HelseIdConfig HelseId { get; set; }
    public required SelvbetjeningConfig Selvbetjening { get; set; }
    public required ClientConfig Client { get; set; }
}

internal class HelseIdConfig
{
    public required string Authority { get; set; }
    public required bool UseDPoP { get; set; }
}

internal class SelvbetjeningConfig
{
    public required string ApiUri { get; set; }
    public required string ClientStatusEndpoint { get; set; }
    public required string ClientSecretEndpoint { get; set; }
    public required string ClientEndpoint { get; set; }

    public string ClientUri => GetEndpointUri(ClientEndpoint);
    public string ClientStatusUri => GetEndpointUri(ClientStatusEndpoint);
    public string ClientSecretUri => GetEndpointUri(ClientSecretEndpoint);

    private string GetEndpointUri(string endpointPath) => new Uri(new Uri(ApiUri), endpointPath).ToString();
}

internal class ClientConfig
{
    public required string ClientId { get; set; }
    public required string Jwk { get; set; }
}
