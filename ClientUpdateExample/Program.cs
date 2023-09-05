using Auth;
using Auth.Utils;
using ClientUpdateExample.Configuration;
using Common.Models;
using Common.Models.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class Program
{
    private const string SelvbetjeningClientScope = "nhn:selvbetjening/client";

    private static async Task Main()
    {
        var config = GetConfig();

        using var authHttpClient = new AuthHttpClient(config.HelseId.UseDPoP ? new JwkWithMetadata(config.Client.Jwk, "", SecurityAlgorithms.RsaSha512) : null);

        var update = new ClientUpdate
        {
            ApiScopes = new[] { SelvbetjeningClientScope, "a_new_scope" },
            AudienceSpecificClientClaims = null,
            ChildOrganizationNumbers = null,
            PostLogoutRedirectUris = null,
            RedirectUris = null,
        };

        try
        {
            await UpdateClient(authHttpClient, config, update);
            await Out("Update complete");
        }
        catch (Exception ex)
        {
            await Out($"Update failed: {ex.Message}");
        }

        // *** Uncomment to update the client secret ***
        // var newJwk = KeyGenerator.GenerateJwk();
        // await UpdateClientSecret(config, authHttpClient, newJwk);
    }

    private static async Task UpdateClient(AuthHttpClient authHttpClient, Config config, ClientUpdate update)
    {
        string accessToken = await GetAccessToken(config);

        await authHttpClient.Put(config.Selvbetjening.ClientUri, update, accessToken: accessToken);
    }

    private static async Task UpdateClientSecret(Config config, AuthHttpClient authHttpClient, (string publicJwk, string publicAndPrivateJwk) newJwk)
    {
        var clientSecretUpdateResponse = await UpdateClientSecret(authHttpClient, config, newJwk.publicJwk);

        await Out($"New client secret expiration: {clientSecretUpdateResponse.Expiration}");
    }

    private static async Task<ClientSecretUpdateResponse> UpdateClientSecret(AuthHttpClient authHttpClient, Config config, string newPublicJwk)
    {
        string accessToken = await GetAccessToken(config);

        return await authHttpClient.Post<string, ClientSecretUpdateResponse>(config.Selvbetjening.ClientSecretUri, newPublicJwk, accessToken: accessToken);
    }

    private static async Task<string> GetAccessToken(Config config)
    {
        var clientData = new SystemClientData
        {
            Authority = config.HelseId.Authority,
            ClientId = config.Client.ClientId,
            Jwk = new JwkWithMetadata(config.Client.Jwk, "", SecurityAlgorithms.RsaSha512),
            Scopes = new[] { SelvbetjeningClientScope },
            UseDPoP = config.HelseId.UseDPoP,
        };

        var clientCredentialsTokens = await GetClientCredentialsTokens(clientData);

        return clientCredentialsTokens.AccessToken;
    }

    private static async Task<Tokens> GetClientCredentialsTokens(SystemClientData clientData)
    {
        using var auth = new SystemAuthenticator(clientData);

        return await auth.GetTokens();
    }

    private static async Task Out(string message)
    {
        await Console.Out.WriteLineAsync(message);
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