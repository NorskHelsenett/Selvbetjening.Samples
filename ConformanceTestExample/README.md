# Conformance Test Example

A conformance test checks that your software authenticates the way the HelseID security profile
requires. The session hands out an authority URI of its own; your client authenticates against that
instead of HelseID, and the service examines what it saw. Nothing is simulated — the token request
your software makes is the thing being tested.

This examples covers a machine-to-machine client.

## Before you start

You need three things, and they have to belong together:

1. A **client system** in Selvbetjening for HelseID.
2. An **API key for conformance testing**, generated from the `Samsvarstesting` tab of that client
   system. This is a different key from the one on the `Automation` tab.
3. An existing **HelseID client created from that same client system**, and its private key.
   Starting a session for a client belonging to a different client system is rejected.

Put the values in `appsettings.Local.json` next to `appsettings.json`, which is gitignored:

```
{
  "Selvbetjening": {
    "ConformanceTestApiKey": "[PASTE here]"
  },
  "Client": {
    "ClientId": "[PASTE here]",
    "Jwk": "[PASTE the private JWK here]",
    "Scopes": [ "[PASTE each scope you use here]" ]
  }
}
```

## What the sample does

1. Starts a session with `POST /v1/conformance-test-sessions`, authenticating with the `api-key`
   header, and receives a session id and the authority URI to use
2. Requests an access token with the client credentials grant against that authority — this is the
   step being examined. Tokens issued by a session cannot be used against APIs. The session accepts
   requests that HelseID would reject, so receiving a token does not mean the request was correct
3. Ends the session with `POST /v1/conformance-test-sessions/{sessionId}/end`, which returns the
   verdict and the checks behind it
4. Prints each request the test observed, with the outcome of every check

`Program.cs` also has two examples for endpoints that are not required. They are off by default;
set the constant at the top of the file to `true` to run one:

- `EnableResetExample` resets the session with `POST /v1/conformance-test-sessions/{sessionId}/reset`,
  runs the client again and ends the session. The session id and authority URI stay the same, so
  you can test again, for example after fixing your client, without changing its configuration
- `EnableGetExample` fetches the session with `GET /v1/conformance-test-sessions/{sessionId}`, for
  reading the results at a later time. Sessions are not stored forever

A warning prevents a test from passing, not only a failure. The same results are shown on the
client system's `Samsvarstesting` tab in Selvbetjening for HelseID.
