using Auth.Utils;
using Common.Models;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Auth;

public class SystemAuthenticator : IDisposable
{
    private readonly SystemClientData _clientData;
    private readonly HttpClient _httpClient;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _oidcConfigManager;

    public SystemAuthenticator(SystemClientData clientData, bool logHttp = false)
    {
        _clientData = clientData;
        _httpClient = logHttp
            ? new HttpClient(new RawHttpLoggingHandler(new HttpClientHandler()))
            : new HttpClient();
        _oidcConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{_clientData.Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()
        );
    }

    public async Task<Tokens> GetTokens()
    {
        var oidcConfig = await GetOidcConfig();

        var request = CreateClientCredentialsTokenRequest(oidcConfig.TokenEndpoint);

        var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(request);

        if (RequiresDPoPNonce(tokenResponse))
        {
            request = CreateClientCredentialsTokenRequest(oidcConfig.TokenEndpoint, dPoPNonce: tokenResponse.DPoPNonce);
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

    private bool RequiresDPoPNonce(TokenResponse tokenResponse)
    {
        return _clientData.UseDPoP && tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce);
    }

    private ClientCredentialsTokenRequest CreateClientCredentialsTokenRequest(string tokenEndpoint, string? dPoPNonce = null)
    {
        var request = CreateTokenRequestBase(tokenEndpoint, dPoPNonce);
        return new ClientCredentialsTokenRequest
        {
            Address = request.Address,
            ClientAssertion = request.ClientAssertion,
            ClientId = request.ClientId,
            ClientCredentialStyle = request.ClientCredentialStyle,
            DPoPProofToken = request.DPoPProofToken,
            Scope = string.Join(" ", _clientData.Scopes),
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
        };
    }

    private ProtocolRequest CreateTokenRequestBase(string tokenEndpoint, string? dPoPNonce = null)
    {
        return new ProtocolRequest
        {
            Address = tokenEndpoint,
            ClientAssertion = ClientAssertionBuilder.Build(_clientData.ClientId, _clientData.Jwk.PublicAndPrivateValue, _clientData.Authority),
            ClientId = _clientData.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            DPoPProofToken = _clientData.UseDPoP ? DPoPProofBuilder.CreateDPoPProof(tokenEndpoint, "POST", _clientData.Jwk, dPoPNonce: dPoPNonce) : null,
        };
    }

    private async Task<OpenIdConnectConfiguration> GetOidcConfig()
    {
        return await _oidcConfigManager.GetConfigurationAsync();
    }

    public void Dispose()
    {
        using (_httpClient) { }
    }
}
