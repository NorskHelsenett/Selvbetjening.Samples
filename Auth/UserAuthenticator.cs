using Auth.Utils;
using Common.Models;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.DPoP;
using Microsoft.Extensions.Logging;

namespace Auth;

public sealed class UserAuthenticator : IDisposable
{
    private readonly UserClientData _clientData;
    private readonly HttpClient _httpClient;
    private readonly OidcClient _oidcClient;

    public UserAuthenticator(UserClientData clientData, string htmlTitle, string htmlBody, bool logHttp = false)
    {
        _clientData = clientData;
        _httpClient = logHttp
            ? new HttpClient(new RawHttpLoggingHandler(new HttpClientHandler()))
            : new HttpClient();
        _oidcClient = CreateOidcClient(htmlTitle, htmlBody);
    }

    public async Task<ResourceTokens> LoginAndGetTokens(string[]? resources = null)
    {
        var resourceTokens = new List<ResourceToken>();
        var latestRefreshToken = string.Empty;

        ValidateResources(resources);

        // 1. Logging in the user and retrieving the access token for API 1 and refresh token
        // ///////////////////////
        // Uses OidcClient to login the user and retrieve tokens.
        // Pushed Authorization Request (PAR) is automatically used by OidcClient.
        // Use the Resource-parameter to indicate which APIs you want tokens for
        // Use the Scope-parameter to indicate which scopes you want for these APIs

        var resourcesToGetTokensFor = resources?.Length > 0 ? resources : _clientData.Resources.Select(r => r.Name).ToArray();

        var firstResource = resourcesToGetTokensFor.First();

        var loginRequest = new LoginRequest
        {
            // We want scopes for API 1 in the initial access token, so specify only the first resource.
            BackChannelExtraParameters = new Parameters([new KeyValuePair<string, string>("resource", firstResource)]),
        };

        // Performs the authorization code flow, opening the 'authorize' endpoint in the browser and retrieving the initial tokens.
        var loginResult = await _oidcClient.LoginAsync(loginRequest);

        if (loginResult.IsError)
        {
            throw new Exception(loginResult.Error);
        }

        resourceTokens.Add(new ResourceToken(firstResource, loginResult.AccessToken));

        latestRefreshToken = loginResult.RefreshToken;

        // 2. Using the refresh token to get an access token for API N
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

        return new ResourceTokens([.. resourceTokens], latestRefreshToken);
    }

    public async Task<ResourceTokens> GetTokens(string refreshToken, string resource)
    {
        ValidateResources([resource]);

        var refreshResult = await _oidcClient.RefreshTokenAsync(refreshToken, new Parameters { { "resource", resource } });
        if (refreshResult.IsError)
        {
            throw new Exception($"Token refresh failed: {refreshResult.Error}: {refreshResult.ErrorDescription}");
        }

        return new ResourceTokens(
            [new ResourceToken(resource, refreshResult.AccessToken)],
            refreshResult.RefreshToken);
    }

    private OidcClient CreateOidcClient(string htmlTitle, string htmlBody)
    {
        var resourceScope = string.Join(" ", _clientData.Resources.Select(r => string.Join(" ", r.Scopes)));

        var redirectUri = $"{_clientData.RedirectHost}{_clientData.RedirectPath}";
        var scope = $"openid offline_access {resourceScope}";

        var options = new OidcClientOptions
        {
            Authority = _clientData.Authority,
            ClientId = _clientData.ClientId,
            RedirectUri = redirectUri,
            Scope = scope,
            Resource = _clientData.Resources.Select(r => r.Name).ToArray(),
            GetClientAssertionAsync = () => Task.FromResult(ClientAssertionBuilder.Build(_clientData.ClientId, _clientData.Jwk.PublicAndPrivateValue, _clientData.Authority)),
            Browser = new SystemBrowserRunner(htmlTitle, htmlBody),
            LoadProfile = false,
            LoggerFactory = LoggerFactory.Create(builder => builder
                .AddConsole()
                // Set LogLevel to Debug or Trace to see details in the console
                .SetMinimumLevel(LogLevel.Information)
            ),
        };

        if (_clientData.UseDPoP)
        {
            var proofKey = _clientData.Jwk.PublicAndPrivateValue;

            options.ConfigureDPoP(proofKey);
        }

        return new OidcClient(options);
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
