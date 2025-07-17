namespace Common.Models;

public class ResourceToken(string resource, string accessToken)
{
    public string Resource { get; } = resource;
    public string AccessToken { get; } = accessToken;
}