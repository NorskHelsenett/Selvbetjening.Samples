using Auth.Utils;
using Common.Models;
using IdentityModel;
using IdentityModel.Client;

namespace Auth;

public class SystemAuthenticator : IDisposable
{
    private readonly string _authority;
    private readonly HttpClient _httpClient;
    private DiscoveryDocumentResponse? _cachedDiscoveryDocument;

    public SystemAuthenticator(string authority)
    {
        _authority = authority;
        _httpClient = new HttpClient();
    }

    public async Task<Tokens> GetTokens(string clientId, string publicAndPrivateJwk, string[] scopes)
    {
        var disco = await GetDisco();

        var request = new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientAssertion = ClientAssertionBuilder.GetClientAssertion(clientId, publicAndPrivateJwk, _authority),
            ClientId = clientId,
            Scope = string.Join(" ", scopes),
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
            ClientCredentialStyle = ClientCredentialStyle.PostBody
        };

        var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(request);

        if (tokenResponse.IsError)
        {
            throw new Exception($"Failed getting client credentials tokens: {tokenResponse.Error}");
        }

        return new Tokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);
    }

    private async Task<DiscoveryDocumentResponse> GetDisco()
    {
        if (_cachedDiscoveryDocument != null)
        {
            return _cachedDiscoveryDocument;
        }

        var disco = await _httpClient.GetDiscoveryDocumentAsync(_authority);

        if (disco.IsError)
        {
            throw new Exception($"Failed getting discovery document: {disco.Error}");
        }

        _cachedDiscoveryDocument = disco;

        return _cachedDiscoveryDocument;
    }

    public void Dispose()
    {
        using (_httpClient) { }
    }
}