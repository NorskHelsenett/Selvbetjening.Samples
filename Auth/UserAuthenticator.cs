using System.Security.Cryptography;
using System.Text;
using Auth.Utils;
using Common.Models;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient.DPoP;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Auth;

public sealed class UserAuthenticator : IDisposable
{
    private readonly UserClientData _clientData;
    private readonly HttpClient _httpClient;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _oidcConfigManager;

    private string _tokenEndpoint = string.Empty;

    public UserAuthenticator(UserClientData clientData)
    {
        _clientData = clientData;
        _httpClient = new HttpClient();
        _oidcConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{_clientData.Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()
        );
    }

    public async Task<ResourceTokens> LoginAndGetTokens(
        string[]? resources = null,
        string htmlTitle = "Login",
        string htmlBody = "<h1>You can now return to the application.</h1>")
    {
        var resourceTokens = new List<ResourceToken>();
        string latestRefreshToken = string.Empty;

        ValidateResources(resources);

        var oidcConfig = await _oidcConfigManager.GetConfigurationAsync();
        _tokenEndpoint = oidcConfig.TokenEndpoint;

        // 1. Logging in the user
        // ///////////////////////
        // Perfom user login, uses the /authorize endpoint in HelseID
        // Use the Resource-parameter to indicate which APIs you want tokens for
        // Use the Scope-parameter to indicate which scopes you want for these APIs

        var clientAssertionPayload = ClientAssertionBuilder.GetClientAssertion(_clientData.ClientId, _clientData.Jwk.PublicAndPrivateValue, _clientData.Authority, _clientData.OrganizationNumber);

        string resourceScope = string.Join(" ", _clientData.Resources.Select(r => string.Join(" ", r.Scopes)));
        string[] configuredResources = _clientData.Resources.Select(r => r.Name).ToArray();
        string[] resourcesToGetTokensFor = resources?.Length > 0 ? resources : configuredResources;

        var redirectUri = $"{_clientData.RedirectHost}{_clientData.RedirectPath}";
        var scope = $"openid offline_access {resourceScope}";

        var options = new OidcClientOptions
        {
            Authority = _clientData.Authority,
            LoadProfile = false,
            RedirectUri = redirectUri,
            ClientAssertion = clientAssertionPayload,
            LoggerFactory = LoggerFactory.Create(builder => builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information)
            )
        };

        if (_clientData.UseDPoP)
        {
            var proofKey = _clientData.Jwk.PublicAndPrivateValue;

            options.ConfigureDPoP(proofKey);
        }

        var oidcClient = new OidcClient(options);

        var authorizeState = await PrepareLoginWithPar(oidcClient, oidcConfig, scope, configuredResources);

        var browserOptions = new BrowserOptions(authorizeState.StartUrl, redirectUri);
        using var browserRunner = new SystemBrowserRunner(htmlTitle, htmlBody);
        var browserResult = await browserRunner.InvokeAsync(browserOptions, default);

        // 2. Retrieving an access token for API 1, and a refresh token
        ///////////////////////////////////////////////////////////////////////
        // User login has finished, now we want to request tokens from the /token endpoint
        // We add a Resource parameter indication that we want scopes for API 1

        var firstResource = resourcesToGetTokensFor.First();

        var parameters = new Parameters
            {
                { "resource", firstResource }
            };

        var loginResult = await oidcClient.ProcessResponseAsync(browserResult.Response, authorizeState, parameters);

        if (loginResult.IsError)
        {
            throw new Exception(loginResult.Error);
        }

        resourceTokens.Add(new ResourceToken(firstResource, loginResult.AccessToken));

        latestRefreshToken = loginResult.RefreshToken;

        // 3. Using the refresh token to get an access token for API N
        //////////////////////////////////////////////////////////////
        // Now we want a second access token to be used for API N
        // Again we use the /token-endpoint, but now we use the refresh token
        // The Resource parameter indicates that we want a token for API N.
        // We won't use a refresh token to get an access token for the first
        // resource – we received that token logging in.

        var resourcesToGetWithRefreshTokens = resourcesToGetTokensFor.Skip(1);

        foreach (var resource in resourcesToGetWithRefreshTokens)
        {
            var tokens = await GetTokens(latestRefreshToken, resource);

            resourceTokens.Add(tokens.Tokens.Single());

            latestRefreshToken = tokens.RefreshToken;
        }

        return new ResourceTokens(resourceTokens.ToArray(), latestRefreshToken);
    }

    public async Task<ResourceTokens> GetTokens(string refreshToken, string resource)
    {
        ValidateResources([resource]);

        var refreshTokenRequest = CreateRefreshTokenRequest(refreshToken, resource);

        var tokenResponse = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

        if (_clientData.UseDPoP && tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce))
        {
            refreshTokenRequest = CreateRefreshTokenRequest(refreshToken, resource, dPoPNonce: tokenResponse.DPoPNonce);
            tokenResponse = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);
        }

        if (tokenResponse.IsError)
        {
            throw new Exception($"{tokenResponse.Error} {tokenResponse.ErrorDescription}");
        }

        if (tokenResponse.AccessToken == null)
        {
            throw new Exception("Access token is not set.");
        }

        if (tokenResponse.RefreshToken == null)
        {
            throw new Exception("Refresh token is not set.");
        }

        return new ResourceTokens(
            [new ResourceToken(resource, tokenResponse.AccessToken)],
            tokenResponse.RefreshToken);
    }

    private async Task<AuthorizeState> PrepareLoginWithPar(OidcClient oidcClient, OpenIdConnectConfiguration oidcConfig, string scope, string[] resources)
    {
        var state = await oidcClient.PrepareLoginAsync();

        var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(state.CodeVerifier));
        var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        var parRequest = new PushedAuthorizationRequest
        {
            // NOTE: OpenIdConnectConfiguration does currently not include a property for this endpoint
            Address = (string)oidcConfig.AdditionalData["pushed_authorization_request_endpoint"],
            ClientId = _clientData.ClientId,
            ClientAssertion = oidcClient.Options.ClientAssertion,
            RedirectUri = oidcClient.Options.RedirectUri,
            Scope = scope,
            Resource = resources,
            ResponseType = OidcConstants.ResponseTypes.Code,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = OidcConstants.CodeChallengeMethods.Sha256,
            State = state.State,
        };

        var parResponse = await _httpClient.PushAuthorizationAsync(parRequest);
        if (parResponse.IsError)
        {
            throw new Exception($"Failed PAR: {parResponse.ErrorType}: {parResponse.Error}");
        }

        // Override start URL with URL using PAR
        state.StartUrl = $"{oidcConfig.AuthorizationEndpoint}?client_id={_clientData.ClientId}&request_uri={parResponse.RequestUri}"; ;

        return state;
    }

    private RefreshTokenRequest CreateRefreshTokenRequest(string refreshToken, string resource, string? dPoPNonce = null)
    {
        return new RefreshTokenRequest
        {
            Address = _tokenEndpoint,
            ClientId = _clientData.ClientId,
            ClientAssertion = ClientAssertionBuilder.GetClientAssertion(_clientData.ClientId, _clientData.Jwk.PublicAndPrivateValue, _clientData.Authority, _clientData.OrganizationNumber),
            GrantType = OidcConstants.GrantTypes.RefreshToken,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            RefreshToken = refreshToken,
            Resource = new List<string> { resource },
            DPoPProofToken = _clientData.UseDPoP ? DPoPProofBuilder.CreateDPoPProof(_tokenEndpoint, "POST", _clientData.Jwk, dPoPNonce: dPoPNonce) : null,
        };
    }

    private void ValidateResources(string[]? resources)
    {
        if (resources != null)
        {
            if (resources.Length == 0)
            {
                throw new ArgumentException("When specifying resources to get tokens for, the list cannot be empty.");
            }

            foreach (string resource in resources)
            {
                if (!_clientData.Resources.Any(cr => cr.Name == resource))
                {
                    throw new ArgumentException($"The resource '{resource}' has not been configured.");
                }
            }
        }
    }

    public void Dispose()
    {
        using (_httpClient) { }
    }
}