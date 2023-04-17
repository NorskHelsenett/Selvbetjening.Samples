namespace Common.Models;

public class ResourceToken
{
    public ResourceToken(string resource, string accessToken)
    {
        Resource = resource;
        AccessToken = accessToken;
    }

    public string Resource { get; set; }
    public string AccessToken { get; set; }
}