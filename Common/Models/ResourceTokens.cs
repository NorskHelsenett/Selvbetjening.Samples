namespace Common.Models;

public class ResourceTokens
{
    public ResourceTokens(ResourceToken[] tokens, string refreshToken)
    {
        Tokens = tokens;
        RefreshToken = refreshToken;
    }

    public ResourceToken[] Tokens { get; set; }
    public string RefreshToken { get; set; }
}