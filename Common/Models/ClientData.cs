namespace Common.Models;

public class ClientData
{
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string Jwk { get; set; }
    public required string RedirectHost { get; set; }
    public required string RedirectPath { get; set; }
    public required Resource[] Resources { get; set; }
    public string? OrganizationNumber { get; set; }
}