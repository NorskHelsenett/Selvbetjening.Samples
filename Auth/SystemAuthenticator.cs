using Auth.Utils;
using Common.Models;
using IdentityModel;
using IdentityModel.Client;

namespace Auth;

public class SystemAuthenticator : IDisposable
{
    private readonly SystemClientData _clientData;
    private readonly HttpClient _httpClient;
    private DiscoveryDocumentResponse? _cachedDiscoveryDocument;

    public SystemAuthenticator(SystemClientData clientData)
    {
        _clientData = clientData;
        _httpClient = new HttpClient();
    }

    public async Task<Tokens> GetTokens()
    {
        var disco = await GetDisco();

        var request = CreateClientCredentialsTokenRequest(disco.TokenEndpoint!);

        var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(request);

        if (_clientData.UseDPoP && tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce))
        {
            request = CreateClientCredentialsTokenRequest(disco.TokenEndpoint!, dPoPNonce: tokenResponse.DPoPNonce);
            tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(request);
        }

        if (tokenResponse.IsError)
        {
            throw new Exception($"Failed getting client credentials tokens: {tokenResponse.Error}");
        }

        if (_clientData.UseDPoP && tokenResponse.TokenType != "DPoP")
        {
            throw new Exception($"Expected 'DPoP' token type, but received '{tokenResponse.TokenType}' token");
        }

        return new Tokens(tokenResponse.AccessToken!, tokenResponse.RefreshToken!);
    }

    private ClientCredentialsTokenRequest CreateClientCredentialsTokenRequest(string tokenEndpoint, string? dPoPNonce = null)
    {
        return new ClientCredentialsTokenRequest
        {
            Address = tokenEndpoint,
            ClientAssertion = ClientAssertionBuilder.GetClientAssertion(_clientData.ClientId, _clientData.Jwk.PublicAndPrivateValue, _clientData.Authority),
            ClientId = _clientData.ClientId,
            Scope = string.Join(" ", _clientData.Scopes),
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            DPoPProofToken = _clientData.UseDPoP ? DPoPProofBuilder.CreateDPoPProof(tokenEndpoint, "POST", _clientData.Jwk, dPoPNonce: dPoPNonce) : null,
        };
    }

    private async Task<DiscoveryDocumentResponse> GetDisco()
    {
        if (_cachedDiscoveryDocument != null)
        {
            return _cachedDiscoveryDocument;
        }

        var disco = await _httpClient.GetDiscoveryDocumentAsync(_clientData.Authority);

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