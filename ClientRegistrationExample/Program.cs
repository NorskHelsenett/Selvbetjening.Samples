using Auth;
using Auth.Utils;
using ClientRegistrationExample.Configuration;
using Common.Crypto;
using Common.Models;
using Common.Models.Response;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program
{
    private const string SelvbetjeningResource = "nhn:selvbetjening";

    private static async Task Main(string[] args)
    {
        var config = GetConfig();

        var jwk = KeyGenerator.GenerateJwk();

        using var authHttpClient = new AuthHttpClient();
        string redirectUri = $"http://localhost:{config.RedirectPort}";
        string redirectPath = $"/{config.RedirectPath}";

        /*
         * Step 1: Create and submit the client draft
         */

        string clientId = (await SubmitClientDraft(config, jwk.publicJwk, authHttpClient)).ClientId;

        /*
         * Step 2: Let the user log in and confirm the client draft
         */

        string confirmationStatus = await ConfirmClientDraft(config, clientId, redirectUri, redirectPath);

        if (confirmationStatus != "Success")
        {
            await Out($"Confirmation status is {confirmationStatus}. Aborting ...");
            return;
        }

        /*
         * Step 3: Check the status of the client
         */

        var clientStatus = await GetClientStatus(authHttpClient, config, clientId, jwk.publicAndPrivateJwk);

        if (clientStatus != OverallClientStatus.Active)
        {
            await Out($"Client status is {clientStatus}. Aborting ...");
            return;
        }

        /*
         * Step 4: Get an access token for each resource (audience)
         */

        await FetchAndPrintAccessTokens(config, jwk.publicAndPrivateJwk, clientId, redirectUri, redirectPath);

        /*
         * Step 5 (later): Update the client secret
         */

        var newJwk = KeyGenerator.GenerateJwk();

        var clientSecretUpdateResponse = await UpdateClientSecret(authHttpClient, config, clientId, jwk.publicAndPrivateJwk, newJwk.publicJwk);

        await Out($"New client secret expiration: {clientSecretUpdateResponse.Expiration}");

        await Out("Done ...");
    }

    private static async Task<ClientDraftResponse> SubmitClientDraft(Config config, string publicJwk, AuthHttpClient authHttpClient)
    {
        var clientDraft = new ClientDraft(config.OrganizationNumber, publicJwk, config.ApiScopes);

        return await authHttpClient.Post<ClientDraft, ClientDraftResponse>(
            config.ClientDraftUri, clientDraft,
            headers: new Dictionary<string, string> { [config.ClientDraftApiKeyHeader] = config.ClientDraftApiKey });
    }

    private static async Task<string> ConfirmClientDraft(Config config, string clientId, string redirectUri, string redirectPath)
    {
        string confirmationUri = config.ConfirmationUri.Replace("<client_id>", clientId).Replace("<port>", config.RedirectPort.ToString()).Replace("<path>", redirectPath);
        string confirmationStatus = "";

        using (var browserRunner = new BrowserRunner(redirectUri, $"/{config.RedirectPath}", config.HtmlTitle, config.HtmlBody))
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
        var clientCredentialsTokens = await GetClientCredentialsTokens(config.Authority, clientId, publicAndPrivateJwk, string.Join(" ", config.ApiScopes.Where(s => s.StartsWith(SelvbetjeningResource))));

        var clientStatusResponse = await authHttpClient.Get<ClientStatusResponse>(config.ClientStatusUri, accessToken: clientCredentialsTokens.AccessToken);

        return clientStatusResponse.Status;
    }

    private static async Task<ClientSecretUpdateResponse> UpdateClientSecret(AuthHttpClient authHttpClient, Config config, string clientId, string existingPublicAndPrivateJwk, string newPublicJwk)
    {
        var clientCredentialsTokens = await GetClientCredentialsTokens(config.Authority, clientId, existingPublicAndPrivateJwk, string.Join(" ", config.ApiScopes.Where(s => s.StartsWith(SelvbetjeningResource))));

        var clientSecretUpdateResponse = await authHttpClient.Post<string, ClientSecretUpdateResponse>(config.ClientSecretUri, newPublicJwk, accessToken: clientCredentialsTokens.AccessToken);

        return clientSecretUpdateResponse;
    }

    private static async Task<Tokens> GetClientCredentialsTokens(string authority, string clientId, string publicAndPrivateJwk, string scope)
    {
        using var auth = new SystemAuthenticator(authority);

        return await auth.GetTokens(clientId, publicAndPrivateJwk, scope);
    }

    private static async Task FetchAndPrintAccessTokens(Config config, string publicAndPrivateJwk, string clientId, string redirectUri, string redirectPath)
    {
        var clientData = new ClientData
        {
            ClientId = clientId,
            Authority = config.Authority,
            Jwk = publicAndPrivateJwk,
            RedirectHost = redirectUri,
            RedirectPath = redirectPath,
            Resources = GetResources(config.ApiScopes),
        };

        using var auth = new UserAuthenticator(clientData);

        var initialResourceTokens = await LoginAndGetTokens(
            auth,
            clientData.Resources.Where(r => r.Name == SelvbetjeningResource).Select(r => r.Name).ToArray(),
            config.HtmlTitle,
            config.HtmlBody);

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

    private static Resource[] GetResources(string[] apiScopes)
    {
        var dict = new Dictionary<string, List<string>>();

        foreach (string scope in apiScopes)
        {
            string[] parts = scope.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string resourceName = parts[0];
            string scopeName = parts[1];

            if (!dict.TryGetValue(resourceName, out var scopeList))
            {
                scopeList = new List<string>();
                dict.Add(resourceName, scopeList);
            }

            scopeList.Add(scopeName);
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
        return confirmationResult.Substring(1).Split("&").Select(s => s.Split("=")).ToDictionary(s => s[0], s => s[1]);
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

    private static JsonSerializerOptions _jsonSerializerOptions;
}