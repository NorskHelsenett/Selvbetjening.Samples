namespace Common.Models;

public class ResourceTokens(ResourceToken[] tokens, string refreshToken)
{
    public ResourceToken[] Tokens { get; } = tokens;
    public string RefreshToken { get; } = refreshToken;
}