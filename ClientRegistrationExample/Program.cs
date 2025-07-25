﻿using Auth;
using Auth.Utils;
using ClientRegistrationExample.Configuration;
using Common.Crypto;
using Common.Models;
using Common.Models.Response;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Configuration;

namespace ClientRegistrationExample;

internal static class Program
{
    private const string SelvbetjeningResource = "nhn:selvbetjening";

    private static async Task Main()
    {
        var config = GetConfig();

        var jwk = KeyGenerator.GenerateRsaJwk();

        using var authHttpClient = new AuthHttpClient(config.HelseId.UseDPoP ? jwk : null);
        string redirectHost = $"http://localhost:{config.LocalHttpServer.RedirectPort}";
        string redirectPath = $"/{config.LocalHttpServer.RedirectPath}";
        string redirectUri = $"{redirectHost}{redirectPath}";


        /*
         * Step 1: Create and submit the client draft
         */

        var clientId = (await SubmitClientDraft(config, jwk.PublicValue, redirectUri, authHttpClient)).ClientId;

        /*
         * Step 2: Let the user log in and confirm the client draft
         */

        var confirmationStatus = await ConfirmClientDraft(config, clientId, redirectHost);

        if (confirmationStatus != "Success")
        {
            await Out($"Confirmation status is {confirmationStatus}. Aborting ...");
            return;
        }

        await Out("Waiting 20 seconds for HelseID cache to load the client configuration...");

        // Wait for the client configuration to be loaded into HelseID's runtime cache (worst case 20 seconds for the test environment)
        // HelseID will improve this in the future
        await Task.Delay(20000);

        /*
         * Step 3: Check the status of the client
         */

        var clientDataForSelvbetjeningScopes = new SystemClientData
        {
            Authority = config.HelseId.Authority,
            ClientId = clientId,
            Jwk = jwk,
            Scopes = config.ClientDraft.ApiScopes.Where(s => s.StartsWith(SelvbetjeningResource)).ToArray(),
            UseDPoP = config.HelseId.UseDPoP,
        };

        var currentClient = await GetClientStatus(authHttpClient, clientDataForSelvbetjeningScopes, config.Selvbetjening.ClientStatusUri);
        var accessOk = currentClient.ApiScopes.All(s => s.Status == ScopeAccessStatus.Ok) &&
                       currentClient.AudienceSpecificClientClaims.All(c => c.Status == AudienceSpecificClaimStatus.Ok);

        if (!accessOk)
        {
            await Out($"Client status is not OK. Aborting ...");
            return;
        }

        await Out("Client is ready");

        /*
         * Step 4: Get an access token for each resource (audience)
         */

        if (config.App.UserLogin)
        {
            await Out("Logging in user...");
            // Authorization code with PKCE
            await LoginUserAndPrintAccessTokens(config, jwk, clientId, redirectHost, redirectPath);
        }
        else
        {
            // Client credentials
            var clientDataForAllScopes = new SystemClientData
            {
                Authority = config.HelseId.Authority,
                ClientId = clientId,
                Jwk = jwk,
                Scopes = config.ClientDraft.ApiScopes,
                UseDPoP = config.HelseId.UseDPoP,
            };

            await LoginSystemAndPrintAccessTokens(clientDataForAllScopes);
        }

        /*
         * Step 5 (later): Update the client secret
         */

        var newJwk = KeyGenerator.GenerateRsaJwk();

        var clientSecretUpdateResponse = await UpdateClientSecret(authHttpClient, clientDataForSelvbetjeningScopes, newJwk.PublicValue, config.Selvbetjening.ClientSecretUri);

        await Out($"New client secret expiration: {clientSecretUpdateResponse.Expiration}");

        await Out("Done ...");
    }

    private static async Task<ClientDraftResponse> SubmitClientDraft(Config config, string publicJwk, string redirectUri, AuthHttpClient authHttpClient)
    {
        var clientDraft = new ClientDraft(config.ClientDraft.OrganizationNumber, publicJwk, config.ClientDraft.ApiScopes, redirectUri)
        {
            AudienceSpecificClientClaims = config.ClientDraft.AudienceSpecificClientClaims,
            ChildOrganizationNumbers = config.ClientDraft.ChildOrganizationNumbers,
            RedirectUris = config.ClientDraft.RedirectUris,
        };

        return await authHttpClient.Post<ClientDraft, ClientDraftResponse>(
            config.Selvbetjening.ClientDraftUri, clientDraft,
            headers: new Dictionary<string, string> { [config.Selvbetjening.ClientDraftApiKeyHeader] = config.Selvbetjening.ClientDraftApiKey });
    }

    private static async Task<string> ConfirmClientDraft(Config config, string clientId, string redirectHost)
    {
        var confirmationUri = config.Selvbetjening.ConfirmationUri.Replace("<client_id>", clientId);

        await Out("Waiting or user to confirm client...");

        var browserOptions = new BrowserOptions(confirmationUri, new Uri(new Uri(redirectHost), config.LocalHttpServer.RedirectPath).ToString());

        using var browserRunner = new SystemBrowserRunner(config.LocalHttpServer.HtmlTitle, config.LocalHttpServer.HtmlBody);
        var result = await browserRunner.InvokeAsync(browserOptions, default);

        var confirmationDict = QuerystringToDictionary(result.Response);

        var confirmationStatus = confirmationDict["status"];

        await Out($"Status received: {confirmationStatus}");

        return confirmationStatus;
    }

    private static async Task<CurrentClient> GetClientStatus(AuthHttpClient authHttpClient, SystemClientData clientData, string clientStatusUri)
    {
        var clientCredentialsTokens = await GetClientCredentialsTokens(clientData);

        var response = await authHttpClient.Get<CurrentClient>(clientStatusUri, accessToken: clientCredentialsTokens.AccessToken);

        return response;
    }

    private static async Task<ClientSecretUpdateResponse> UpdateClientSecret(AuthHttpClient authHttpClient, SystemClientData clientData, string newPublicJwk, string clientSecretUri)
    {
        var clientCredentialsTokens = await GetClientCredentialsTokens(clientData);

        return await authHttpClient.Post<string, ClientSecretUpdateResponse>(clientSecretUri, newPublicJwk, accessToken: clientCredentialsTokens.AccessToken);
    }

    private static async Task<Tokens> GetClientCredentialsTokens(SystemClientData clientData)
    {
        using var auth = new SystemAuthenticator(clientData);

        return await auth.GetTokens();
    }

    private static async Task LoginUserAndPrintAccessTokens(Config config, JwkWithMetadata jwk, string clientId, string redirectUri, string redirectPath)
    {
        var clientData = new UserClientData
        {
            ClientId = clientId,
            Authority = config.HelseId.Authority,
            Jwk = jwk,
            RedirectHost = redirectUri,
            RedirectPath = redirectPath,
            Resources = GetResources(config.ClientDraft.ApiScopes),
            UseDPoP = config.HelseId.UseDPoP,
        };

        using var auth = new UserAuthenticator(clientData, config.LocalHttpServer.HtmlTitle, config.LocalHttpServer.HtmlBody);

        var initialResourceTokens = await LoginAndGetTokens(auth, [SelvbetjeningResource]);

        if (initialResourceTokens == null)
        {
            throw new Exception("Initial resource tokens are null");
        }

        foreach (var resourceToken in initialResourceTokens.Tokens)
        {
            await PrintAccessToken(resourceToken.Resource, resourceToken.AccessToken);
        }

        var lastRefreshToken = initialResourceTokens.RefreshToken;

        foreach (string resource in clientData.Resources.Where(cr => cr.Name != SelvbetjeningResource).Select(cr => cr.Name))
        {
            var resourceTokens = await GetTokens(auth, lastRefreshToken, resource);

            if (resourceTokens == null)
            {
                throw new Exception($"Could not get tokens for resource '{resource}'.");
            }

            lastRefreshToken = resourceTokens.RefreshToken;

            await PrintAccessToken(resource, resourceTokens.Tokens.First().AccessToken);
        }
    }

    private static async Task LoginSystemAndPrintAccessTokens(SystemClientData clientData)
    {
        var resources = GetResources(clientData.Scopes);

        foreach (var resource in resources)
        {
            var clientDataForResource = new SystemClientData
            {
                Authority = clientData.Authority,
                ClientId = clientData.ClientId,
                Jwk = clientData.Jwk,
                Scopes = resource.Scopes,
                UseDPoP = clientData.UseDPoP,
            };

            var tokens = await GetClientCredentialsTokens(clientDataForResource);

            await PrintAccessToken(resource.Name, tokens.AccessToken);
        }
    }

    private static Resource[] GetResources(string[] apiScopes)
    {
        var dict = new Dictionary<string, List<string>>();

        foreach (var scope in apiScopes)
        {
            var parts = scope.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var resourceName = parts[0];

            if (!dict.TryGetValue(resourceName, out var scopeList))
            {
                scopeList = new List<string>();
                dict.Add(resourceName, scopeList);
            }

            scopeList.Add(scope);
        }

        return dict.Select(kvp => new Resource(kvp.Key, kvp.Value.ToArray())).ToArray();
    }

    private static async Task<ResourceTokens?> LoginAndGetTokens(UserAuthenticator authenticator, string[] resources)
    {
        ResourceTokens? resourceTokens = null;

        try
        {
            resourceTokens = await authenticator.LoginAndGetTokens(resources: resources);
        }
        catch (Exception ex)
        {
            await Out($"Error: {ex.Message}");
        }

        return resourceTokens;
    }

    private static async Task<ResourceTokens> GetTokens(UserAuthenticator authenticator, string refreshToken, string resource)
    {
        ResourceTokens resourceTokens = null!;

        try
        {
            resourceTokens = await authenticator.GetTokens(refreshToken, resource);
        }
        catch (Exception ex)
        {
            await Out($"Error: {ex.Message}");
        }

        return resourceTokens;
    }

    private static Config GetConfig()
    {
        var builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json", optional: false)
                              .AddJsonFile("appsettings.Local.json", optional: true);

        IConfiguration configuration = builder.Build();

        var config = configuration.Get<Config>();

        if (config == null)
        {
            throw new Exception("Config is null.");
        }

        return config;
    }

    private static Dictionary<string, string> QuerystringToDictionary(string confirmationResult)
    {
        return confirmationResult[1..].Split("&").Select(s => s.Split("=")).ToDictionary(s => s[0], s => s[1]);
    }

    private static async Task PrintAccessToken(string resource, string accessToken)
    {
        await Out($"Resource: {resource}");
        await Out("Access token payload:");
        await Out(JwtDecoder.Decode(accessToken));
        await Out("**********************************");
    }

    private static async Task Out(string message)
    {
        await Console.Out.WriteLineAsync(message);
    }
}