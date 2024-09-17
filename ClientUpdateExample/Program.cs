using System.Text.Json;
using System.Text.Json.Serialization;
using Auth;
using Auth.Utils;
using ClientUpdateExample.Configuration;
using Common.Models;
using Common.Models.Response;
using Microsoft.Extensions.Configuration;

namespace ClientUpdateExample;

internal static class Program
{
    private const string SelvbetjeningClientScope = "nhn:selvbetjening/client";

    private static async Task Main()
    {
        var config = GetConfig();

        using var authHttpClient =
            new AuthHttpClient(config.HelseId.UseDPoP ? new JwkWithMetadata(config.Client.Jwk) : null);

        try
        {
            await UpdateClient(authHttpClient, config);
            await Out("Update complete");
        }
        catch (Exception ex)
        {
            await Out($"Update failed: {ex.Message}");
        }


        // *** Uncomment to update the client secret ***
        // var newJwk = KeyGenerator.GenerateRsaJwk();
        // await UpdateClientSecret(config, authHttpClient, newJwk);
    }

    private static async Task<ClientSecretUpdateResponse> UpdateClientSecret(AuthHttpClient authHttpClient,
        Config config, string newPublicJwk)
    {
        string accessToken = await GetAccessToken(config);

        return await authHttpClient.Post<string, ClientSecretUpdateResponse>(config.Selvbetjening.ClientSecretUri,
            newPublicJwk, accessToken: accessToken);
    }

    private static async Task UpdateClient(AuthHttpClient authHttpClient, Config config)
    {
        var accessToken = await GetAccessToken(config);

        var client = await authHttpClient.Get<CurrentClient>(config.Selvbetjening.ClientUri, accessToken: accessToken);
        await Out($"Current redirect URIs: {string.Join(" ", client.RedirectUris)}");

        // Create an update object where everything is unchanged
        var update = new ClientUpdate
        {
            ApiScopes = client.ApiScopes.Select(s => s.Scope).ToArray(),
            AudienceSpecificClientClaims =
                client.AudienceSpecificClientClaims.Select(
                    a => new AudienceSpecificClientClaim(a.ClaimType,
                        a.PendingClaimValue ?? a.ActiveClaimValue ??
                        throw new Exception($"Claim '{a.ClaimType}' has no value"))
                ).ToArray(),
            RedirectUris = client.RedirectUris,
            PostLogoutRedirectUris = client.PostLogoutRedirectUris,
            ChildOrganizationNumbers = client.ChildOrganizationNumbers,
        };

        const string newRedirectUri = "https://new.test/login";

        // Add new redirect uri if not already present
        var updatedRedirectUris = client.RedirectUris.ToList();
        if (!updatedRedirectUris.Contains(newRedirectUri))
        {
            updatedRedirectUris.Add(newRedirectUri);
        }

        // Set updated redirect uris on update object
        update.RedirectUris = updatedRedirectUris.ToArray();

        // Save update
        await authHttpClient.Put(config.Selvbetjening.ClientUri, update, accessToken: accessToken);

        var updatedClient =
            await authHttpClient.Get<CurrentClient>(config.Selvbetjening.ClientUri, accessToken: accessToken);
        await Out($"Updated redirect URIs: {string.Join(" ", updatedClient.RedirectUris)}");
    }

    private static async Task<string> GetAccessToken(Config config)
    {
        var clientData = new SystemClientData
        {
            Authority = config.HelseId.Authority,
            ClientId = config.Client.ClientId,
            Jwk = new JwkWithMetadata(config.Client.Jwk),
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

        if (config.Client.Jwk.First() != '{')
        {
            config.Client.Jwk = File.ReadAllText(config.Client.Jwk);
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