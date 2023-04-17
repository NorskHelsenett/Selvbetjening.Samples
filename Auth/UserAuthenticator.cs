using Auth.Utils;
using Common.Models;
using IdentityModel.Client;
using IdentityModel.OidcClient;

namespace Auth;

public class UserAuthenticator : IDisposable
{
    private readonly ClientData _clientData;
    private readonly HttpClient _httpClient;

    private string _tokenEndpoint = string.Empty;

    public UserAuthenticator(ClientData clientData)
    {
        _clientData = clientData;
        _httpClient = new HttpClient();
    }

    public async Task<ResourceTokens> LoginAndGetTokens(
        string[]? resources = null,
        string htmlTitle = "Login",
        string htmlBody = "<h1>You can now return to the application.</h1>")
    {
        var resourceTokens = new List<ResourceToken>();
        string latestRefreshToken = string.Empty;

        ValidateResources(resources);

        var disco = await _httpClient.GetDiscoveryDocumentAsync(_clientData.Authority);
        if (disco.IsError)
        {
            throw new Exception(disco.Error);
        }

        _tokenEndpoint = disco.TokenEndpoint;

        // 1. Logging in the user
        // ///////////////////////
        // Perfom user login, uses the /authorize endpoint in HelseID
        // Use the Resource-parameter to indicate which APIs you want tokens for
        // Use the Scope-parameter to indicate which scopes you want for these API-s
        var clientAssertionPayload = ClientAssertionBuilder.GetClientAssertion(_clientData.ClientId, _clientData.Jwk, _tokenEndpoint, _clientData.OrganizationNumber);

        string scopes = string.Join(" ", _clientData.Resources.Select(r => string.Join(" ", r.FullScopes)));
        string[] configuredResources = _clientData.Resources.Select(r => r.Name).ToArray();
        string[] resourcesToGetTokensFor = resources?.Length > 0 ? resources : configuredResources;

        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = _clientData.Authority,
            LoadProfile = false,
            RedirectUri = $"{_clientData.RedirectHost}{_clientData.RedirectPath}",
            Scope = $"openid profile offline_access {scopes}",
            ClientId = _clientData.ClientId,
            Resource = configuredResources,
            ClientAssertion = clientAssertionPayload,
        });

        var state = await oidcClient.PrepareLoginAsync();
        using var browserRunner = new BrowserRunner(
            _clientData.RedirectHost,
            _clientData.RedirectPath,
            htmlTitle,
            htmlBody,
            targetUrl: state.StartUrl);
        var response = await browserRunner.PostAndRunUntilCallback();

        // 2. Retrieving an access token for API 1, and a refresh token
        ///////////////////////////////////////////////////////////////////////
        // User login has finished, now we want to request tokens from the /token endpoint
        // We add a Resource parameter indication that we want scopes for API 1
        var firstResource = resourcesToGetTokensFor.First();

        var parameters = new Parameters
            {
                { "resource", firstResource }
            };

        var loginResult = await oidcClient.ProcessResponseAsync(response, state, parameters);

        if (loginResult.IsError)
        {
            throw new Exception(loginResult.Error);
        }

        resourceTokens.Add(new ResourceToken(firstResource, loginResult.AccessToken));

        // 3. Using the refresh token to get an access token for API 2
        //////////////////////////////////////////////////////////////
        // Now we want a second access token to be used for API 2
        // Again we use the /token-endpoint, but now we use the refresh token
        // The Resource parameter indicates that we want a token for API 2.
        latestRefreshToken = loginResult.RefreshToken;

        foreach (var resource in resourcesToGetTokensFor.Skip(1))
        {
            var tokens = await GetTokens(latestRefreshToken, resource);

            resourceTokens.Add(tokens.Tokens.Single());

            latestRefreshToken = tokens.RefreshToken;
        }

        return new ResourceTokens(resourceTokens.ToArray(), latestRefreshToken);
    }

    public async Task<ResourceTokens> GetTokens(string refreshToken, string resource)
    {
        ValidateResources(new[] { resource });

        var refreshTokenRequest = new RefreshTokenRequest
        {
            Address = _tokenEndpoint,
            ClientId = _clientData.ClientId,
            RefreshToken = refreshToken,
            Resource = new List<string> { resource },
            ClientAssertion = ClientAssertionBuilder.GetClientAssertion(_clientData.ClientId, _clientData.Jwk, _tokenEndpoint, _clientData.OrganizationNumber)
        };

        var refreshTokenResult = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

        if (refreshTokenResult.IsError)
        {
            throw new Exception(refreshTokenResult.Error);
        }

        return new ResourceTokens(
            new[] { new ResourceToken(resource, refreshTokenResult.AccessToken) },
            refreshTokenResult.RefreshToken);
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