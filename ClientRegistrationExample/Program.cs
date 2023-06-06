using Auth;
using Auth.Utils;
using ClientRegistrationExample.Configuration;
using Common.Crypto;
using Common.Models;
using Common.Models.Response;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientRegistrationExample;

internal static class Program
{
    private const string SelvbetjeningResource = "nhn:selvbetjening";

    private static async Task Main()
    {
        var config = GetConfig();

        var (publicJwk, publicAndPrivateJwk) = KeyGenerator.GenerateJwk();

        using var authHttpClient = new AuthHttpClient();
        string redirectUri = $"http://localhost:{config.LocalHttpServer.RedirectPort}";
        string redirectPath = $"/{config.LocalHttpServer.RedirectPath}";

        /*
         * Step 1: Create and submit the client draft
         */

        string clientId = (await SubmitClientDraft(config, publicJwk, authHttpClient)).ClientId;

        /*
         * Step 2: Let the user log in and confirm the client draft
         */

        string confirmationStatus = await ConfirmClientDraft(config, clientId, redirectUri, redirectPath);

        if (confirmationStatus != "Success")
        {
            await Out($"Confirmation status is {confirmationStatus}. Aborting ...");
            return;
        }

        // Wait for the client configuration to be loaded into HelseID's runtime cache (worst case 20 seconds for the test environment)
        // HelseID will improve this in the future
        await Task.Delay(20000);

        /*
         * Step 3: Check the status of the client
         */

        var clientStatus = await GetClientStatus(authHttpClient, config, clientId, publicAndPrivateJwk);

        if (clientStatus != OverallClientStatus.Active)
        {
            await Out($"Client status is {clientStatus}. Aborting ...");
            return;
        }

        /*
         * Step 4: Get an access token for each resource (audience)
         */

        if (config.App.UserLogin)
        {
            // Authorization code with PKCE
            await LoginUserAndPrintAccessTokens(config, publicAndPrivateJwk, clientId, redirectUri, redirectPath);
        }
        else
        {
            // Client credentials
            await LoginSystemAndPrintAccessTokens(config, publicAndPrivateJwk, clientId);
        }

        /*
         * Step 5 (later): Update the client secret
         */

        var newJwk = KeyGenerator.GenerateJwk();

        var clientSecretUpdateResponse = await UpdateClientSecret(authHttpClient, config, clientId, publicAndPrivateJwk, newJwk.publicJwk);

        await Out($"New client secret expiration: {clientSecretUpdateResponse.Expiration}");

        await Out("Done ...");
    }

    private static async Task<ClientDraftResponse> SubmitClientDraft(Config config, string publicJwk, AuthHttpClient authHttpClient)
    {
        var clientDraft = new ClientDraft(config.ClientDraft.OrganizationNumber, publicJwk, config.ClientDraft.ApiScopes)
        {
            AudienceSpecificClientClaims = config.ClientDraft.AudienceSpecificClientClaims,
        };

        return await authHttpClient.Post<ClientDraft, ClientDraftResponse>(
            config.Selvbetjening.ClientDraftUri, clientDraft,
            headers: new Dictionary<string, string> { [config.Selvbetjening.ClientDraftApiKeyHeader] = config.Selvbetjening.ClientDraftApiKey });
    }

    private static async Task<string> ConfirmClientDraft(Config config, string clientId, string redirectUri, string redirectPath)
    {
        string confirmationUri = config.Selvbetjening.ConfirmationUri.Replace("<client_id>", clientId).Replace("<port>", config.LocalHttpServer.RedirectPort.ToString()).Replace("<path>", redirectPath);
        string confirmationStatus = "";

        using (var browserRunner = new BrowserRunner(redirectUri, $"/{config.LocalHttpServer.RedirectPath}", config.LocalHttpServer.HtmlTitle, config.LocalHttpServer.HtmlBody))
        {
            string confirmationResult = await browserRunner.RunUntilCallback(confirmationUri);

            var confirmationDict = QuerystringToDictionary(confirmationResult);

            confirmationStatus = confirmationDict["status"];

            await Out($"Status received: {confirmationStatus}");
        }

        return confirmationStatus;
    }

    private static async Task<OverallClientStatus> GetClientStatus(AuthHttpClient authHttpClient, Config config, string clientId, string publicAndPrivateJwk)
    {
        var clientCredentialsTokens = await GetClientCredentialsTokens(config.HelseId.Authority, clientId, publicAndPrivateJwk, config.ClientDraft.ApiScopes.Where(s => s.StartsWith(SelvbetjeningResource)).ToArray());

        var clientStatusResponse = await authHttpClient.Get<ClientStatusResponse>(config.Selvbetjening.ClientStatusUri, accessToken: clientCredentialsTokens.AccessToken);

        return clientStatusResponse.Status;
    }

    private static async Task<ClientSecretUpdateResponse> UpdateClientSecret(AuthHttpClient authHttpClient, Config config, string clientId, string existingPublicAndPrivateJwk, string newPublicJwk)
    {
        var clientCredentialsTokens = await GetClientCredentialsTokens(config.HelseId.Authority, clientId, existingPublicAndPrivateJwk, config.ClientDraft.ApiScopes.Where(s => s.StartsWith(SelvbetjeningResource)).ToArray());

        return await authHttpClient.Post<string, ClientSecretUpdateResponse>(config.Selvbetjening.ClientSecretUri, newPublicJwk, accessToken: clientCredentialsTokens.AccessToken);
    }

    private static async Task<Tokens> GetClientCredentialsTokens(string authority, string clientId, string publicAndPrivateJwk, string[] scopes)
    {
        using var auth = new SystemAuthenticator(authority);

        return await auth.GetTokens(clientId, publicAndPrivateJwk, scopes);
    }

    private static async Task LoginUserAndPrintAccessTokens(Config config, string publicAndPrivateJwk, string clientId, string redirectUri, string redirectPath)
    {
        var clientData = new ClientData
        {
            ClientId = clientId,
            Authority = config.HelseId.Authority,
            Jwk = publicAndPrivateJwk,
            RedirectHost = redirectUri,
            RedirectPath = redirectPath,
            Resources = GetResources(config.ClientDraft.ApiScopes),
        };

        using var auth = new UserAuthenticator(clientData);

        var initialResourceTokens = await LoginAndGetTokens(
            auth,
            new[] { SelvbetjeningResource },
            config.LocalHttpServer.HtmlTitle,
            config.LocalHttpServer.HtmlBody);

        if (initialResourceTokens == null)
        {
            throw new Exception("Initial resource tokens are null");
        }

        foreach (var resourceToken in initialResourceTokens.Tokens)
        {
            await PrintAccessToken(resourceToken.Resource, resourceToken.AccessToken);
        }

        string lastRefreshToken = initialResourceTokens.RefreshToken;

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

    private static async Task LoginSystemAndPrintAccessTokens(Config config, string publicAndPrivateJwk, string clientId)
    {
        var resources = GetResources(config.ClientDraft.ApiScopes);

        foreach (var resource in resources)
        {
            var tokens = await GetClientCredentialsTokens(config.HelseId.Authority, clientId, publicAndPrivateJwk, resource.Scopes);

            await PrintAccessToken(resource.Name, tokens.AccessToken);
        }
    }

    private static Resource[] GetResources(string[] apiScopes)
    {
        var dict = new Dictionary<string, List<string>>();

        foreach (string scope in apiScopes)
        {
            string[] parts = scope.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string resourceName = parts[0];

            if (!dict.TryGetValue(resourceName, out var scopeList))
            {
                scopeList = new List<string>();
                dict.Add(resourceName, scopeList);
            }

            scopeList.Add(scope);
        }

        return dict.Select(kvp => new Resource(kvp.Key, kvp.Value.ToArray())).ToArray();
    }

    private static async Task<ResourceTokens> LoginAndGetTokens(UserAuthenticator authenticator, string[] resources, string htmlTitle, string htmlBody)
    {
        ResourceTokens resourceTokens = null!;

        try
        {
            resourceTokens = await authenticator.LoginAndGetTokens(resources: resources, htmlTitle: htmlTitle, htmlBody: htmlBody);
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

    static Program()
    {
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true,
        };

        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions;
}