using Auth;
using Auth.Utils;
using Common.Models;
using Common.Models.Response;
using ConformanceTestExample.Configuration;
using Microsoft.Extensions.Configuration;

namespace ConformanceTestExample;

/// <summary>
/// Runs a conformance test for a machine-to-machine client through the Selvbetjening API.
///
/// A conformance test observes a real authentication: the session hands out its own authority URI,
/// the client authenticates against that instead of HelseID, and the service examines what it saw.
/// Nothing here simulates a client - the token request in step 3 is the thing being tested.
/// </summary>
internal static class Program
{
    // Serialized as the API expects it: External registers JsonStringEnumConverter with the
    // camel case naming policy.
    private const string MachineToMachineFlow = "machineToMachine";

    private static async Task Main()
    {
        var config = GetConfig();

        using var authHttpClient = new AuthHttpClient();

        /*
         * Step 1: Start a conformance test session for the client
         */

        var session = await StartSession(config, authHttpClient);

        await Out($"Started conformance test session {session.SessionId}");

        /*
         * Step 2: Point the client at the session's authority
         *
         * This is the one piece of configuration that differs from normal operation. In your own
         * software it is the 'authority' setting of whichever OIDC library you use.
         */

        await Out($"Session authority: {session.AuthorityUri}");

        /*
         * Step 3: Run the client as usual
         *
         * For a machine-to-machine client, "as usual" is requesting an access token with the
         * client credentials grant. Note that tokens issued by a conformance test session cannot
         * be used against APIs - they exist so the session has something to examine.
         */

        var clientData = new SystemClientData
        {
            Authority = session.AuthorityUri,
            ClientId = config.Client.ClientId,
            Jwk = new JwkWithMetadata(config.Client.Jwk),
            Scopes = config.Client.Scopes,
            UseDPoP = config.HelseId.UseDPoP,
        };

        using var authenticator = new SystemAuthenticator(clientData);

        var tokens = await authenticator.GetTokens();

        await Out($"Received an access token of {tokens.AccessToken.Length} characters from the session");

        /*
         * Step 4: End the session and read the results
         *
         * Ending the session is what produces the verdict. The same results are also shown in
         * Selvbetjening for HelseID, on the client system's 'Samsvarstesting' tab.
         */

        var results = await EndSession(config, authHttpClient, session.SessionId);

        await PrintResults(results);
    }

    private static async Task PrintResults(ConformanceSessionResult results)
    {
        await Out("");
        await Out(results.Passed ? "Conformance test PASSED" : "Conformance test DID NOT PASS");

        foreach (var request in results.Requests)
        {
            await Out("");
            await Out($"{request.EndpointName}{DescribeTokenRequest(request.TokenRequest)}");

            foreach (var check in request.Checks)
            {
                // Outcome is printed as the API reported it, so a value this sample predates still
                // tells the reader something.
                await Out($"  [{check.Outcome}] {check.Code}: {check.Title.En}");

                foreach (var error in check.Errors)
                {
                    await Out($"      - {error.En}");
                }
            }
        }

        if (!results.Passed)
        {
            await Out("");
            await Out("A warning also prevents a test from passing, not only a failure.");
        }
    }

    private static string DescribeTokenRequest(ConformanceTokenRequestDetails? tokenRequest)
    {
        if (tokenRequest == null)
        {
            return "";
        }

        var resource = tokenRequest.ResourceIndicator == null
            ? ""
            : $", resource={tokenRequest.ResourceIndicator}";

        return $" (grant_type={tokenRequest.GrantType}{resource}, DPoP nonce: {(tokenRequest.DpopNonceIncluded ? "yes" : "no")})";
    }

    private static async Task<ConformanceSessionResult> EndSession(
        Config config,
        AuthHttpClient authHttpClient,
        string sessionId
    )
    {
        return await authHttpClient.Post<object, ConformanceSessionResult>(
            config.Selvbetjening.EndSessionUri(sessionId),
            new { },
            headers: ApiKeyHeader(config));
    }

    private static async Task<ConformanceSessionStartResponse> StartSession(Config config, AuthHttpClient authHttpClient)
    {
        var request = new ConformanceSessionStartRequest(config.Client.ClientId, MachineToMachineFlow);

        return await authHttpClient.Post<ConformanceSessionStartRequest, ConformanceSessionStartResponse>(
            config.Selvbetjening.ConformanceSessionUri,
            request,
            headers: ApiKeyHeader(config));
    }

    private static Dictionary<string, string> ApiKeyHeader(Config config)
    {
        return new Dictionary<string, string>
        {
            [config.Selvbetjening.ConformanceTestApiKeyHeader] = config.Selvbetjening.ConformanceTestApiKey,
        };
    }

    private static Config GetConfig()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

        return configuration.Get<Config>()
            ?? throw new Exception("Could not read appsettings.json");
    }

    private static async Task Out(string message)
    {
        await Console.Out.WriteLineAsync(message);
    }
}
