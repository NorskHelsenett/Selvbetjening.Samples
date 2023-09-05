namespace Common.Models;

public class ClientData
{
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required JwkWithMetadata Jwk { get; set; }
    public bool UseDPoP { get; set; } = false;
}

public class UserClientData : ClientData
{
    public required string RedirectHost { get; set; }
    public required string RedirectPath { get; set; }
    public required Resource[] Resources { get; set; }
    public string? OrganizationNumber { get; set; }
}

public class SystemClientData : ClientData
{
    public required string[] Scopes { get; set; }
}