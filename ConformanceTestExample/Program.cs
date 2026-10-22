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
    private static async Task Main()
    {
        var config = GetConfig();
        var jwk = new JwkWithMetadata(config.Client.Jwk);

        using var authHttpClient = new AuthHttpClient();

        /*
         * Step 1: Start a conformance test session for the client
         */

        var session = await StartSession(config, authHttpClient);

        await Out("-- STARTED SESSION --");
        await PrintSession(session);

        /*
         * Step 2: Point the client at the session's authority
         *
         * This is the one piece of configuration that differs from normal operation. In your own
         * software it is the 'authority' setting of whichever OIDC library you use.
         */

        var clientData = new SystemClientData
        {
            Authority = session.AuthorityUri,
            ClientId = config.Client.ClientId,
            Jwk = jwk,
            Scopes = config.Client.Scopes,
            UseDPoP = config.HelseId.UseDPoP,
        };

        /*
         * Step 3: Run the client as usual
         *
         * For a machine-to-machine client, "as usual" is requesting an access token with the
         * client credentials grant. Note that tokens issued by a conformance test session cannot
         * be used against APIs - they exist so the session has something to examine.
         *
         * The session accepts requests that HelseID would reject, so getting a token here does not
         * mean the request was correct - the checks in step 4 are what tell you that.
         */

        await RunClient(clientData);

        /*
         * Step 4: End the session and read the results
         *
         * Ending the session is what produces the verdict. The same results are also shown in
         * Selvbetjening for HelseID, on the client system's 'Samsvarstesting' tab.
         */

        session = await EndSession(config, authHttpClient, session.SessionId);

        await Out("");
        await Out("-- ENDED SESSION --");
        await PrintSession(session);

        // await RunOptionalSteps(session, clientData, config, authHttpClient);
    }

    private static async Task RunOptionalSteps(
        ConformanceTestSession session,
        SystemClientData clientData,
        Config config,
        AuthHttpClient authHttpClient
    )
    {
        /*
         * Optionally reset the session to run the checks again with the same session authority URI
         *
         * The SessionId and AuthorityUri will be unchanged, so we can reuse the existing configuration
         */

        session = await ResetSession(config, authHttpClient, session.SessionId);

        await Out("");
        await Out("-- RESET SESSION --");
        await PrintSession(session);

        // Note that the configured Authority has not changed after the reset
        await RunClient(clientData);

        session = await EndSession(config, authHttpClient, session.SessionId);

        await Out("");
        await Out("-- ENDED RESET SESSION --");
        await PrintSession(session);

        /*
         * Optionally get the session details at a later time
         *
         * Note that sessions will not be stored forever
         */

        session = await GetSession(config, authHttpClient, session.SessionId);

        await Out("");
        await Out("-- FETCHED SESSION --");
        await PrintSession(session);
    }

    private static async Task PrintSession(ConformanceTestSession session)
    {
        await Out($"Session ID:\t {session.SessionId}");
        await Out($"Authority URI:\t {session.AuthorityUri}");
        await Out($"Client ID:\t {session.ClientId}");
        await Out($"Flow:\t\t {session.Flow}");
        await Out($"Status:\t\t {session.Status}");
        await Out($"Started:\t {session.StartedAt}");

        if (session.EndedAt != null)
        {
            await Out($"Ended:\t\t {session.EndedAt}");
        }

        if (session.Result != null)
        {
            await PrintResults(session.Result);
        }
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

        return
            $" (grant_type={tokenRequest.GrantType}{resource}, DPoP nonce: {(tokenRequest.DpopNonceIncluded ? "yes" : "no")})";
    }

    private static async Task RunClient(SystemClientData clientData)
    {
        using var authenticator = new SystemAuthenticator(clientData);

        await Out("");

        // The session can still fail a request in some cases. Catch it, so that the session is ended
        // and the checks show what went wrong.
        try
        {
            var tokens = await authenticator.GetTokens();
            await Out($"Received an access token of {tokens.AccessToken.Length} characters from the session");
        }
        catch (Exception e)
        {
            await Out($"The client failed: {e.Message}");
        }
    }

    private static async Task<ConformanceTestSession> EndSession(
        Config config,
        AuthHttpClient authHttpClient,
        string sessionId
    )
    {
        return await authHttpClient.Post<ConformanceTestSession>(
            config.Selvbetjening.EndSessionUri(sessionId),
            headers: ApiKeyHeader(config));
    }

    private static async Task<ConformanceTestSession> StartSession(Config config, AuthHttpClient authHttpClient)
    {
        var request = new ConformanceSessionStartRequest(config.Client.ClientId);

        return await authHttpClient.Post<ConformanceSessionStartRequest, ConformanceTestSession>(
            config.Selvbetjening.ConformanceSessionUri,
            request,
            headers: ApiKeyHeader(config));
    }

    private static async Task<ConformanceTestSession> ResetSession(
        Config config,
        AuthHttpClient authHttpClient,
        string sessionId
    )
    {
        return await authHttpClient.Post<ConformanceTestSession>(
            config.Selvbetjening.ResetSessionUri(sessionId),
            headers: ApiKeyHeader(config));
    }

    private static async Task<ConformanceTestSession> GetSession(
        Config config,
        AuthHttpClient authHttpClient,
        string sessionId
    )
    {
        return await authHttpClient.Get<ConformanceTestSession>(
            config.Selvbetjening.SessionUri(sessionId),
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