# Getting started

This tutorial is for creating HelseID clients through [Selvbetjening for HelseID's api](https://api.selvbetjening.test.nhn.no/).

More details can be found at [Utviklerportalen](https://utviklerportal.nhn.no/informasjonstjenester/helseid/tilgang-til-helseid/selvbetjening/docs/client-api/client-api-overviewmd/).

After creating a client system with an api key, which is described in the following section, the flow is like this:

1. Create a client draft using the api
2. Open a web browser and direct the end user to the confirmation page in [Selvbetjening for HelseID](https://selvbetjening.test.nhn.no)
3. The end user confirms and is redirected to your local http server
4. You can check the status of the client's access to the specified scopes
5. When all is good, you can request access tokens for the specified apis

## Client system

A client system must be created in **[Selvbetjening for HelseID ](https://selvbetjening.test.nhn.no/)**

You probably want to enable support for refresh tokens, and it's important to specify which apis (services) are supported by the system. The redirect URI should be set to `http://localhost:1337/callback` when using the default config.

After the client system has been created, go to the 'Automatisering' tab, and generate an api key:

<img width="812" alt="image" src="https://user-images.githubusercontent.com/69471911/234249639-d973749e-27b4-4b50-8a6b-2e4179f46e0e.png">

Now, move into your clone of [appsettings.json](https://github.com/NorskHelsenett/Selvbetjening.Samples/blob/main/ClientRegistrationExample/appsettings.json), and paste the api key. This key is used for authenticating against the [client drafts endpoint](https://ext.selvbetjening.test.nhn.no).

```
{
  ...
  "Selvbetjening": {
    ...
    "ClientDraftApiKeyHeader": "api-key",
    "ClientDraftApiKey": "[PASTE here]"
  }
  ...
}
```

## Creating the client draft

Follow the sample code in [ClientRegistrationExample](https://github.com/NorskHelsenett/Selvbetjening.Samples/tree/main/ClientRegistrationExample)

1. Create the client draft using the `client-drafts` endpoint
2. Direct the end user to Selvbetjening for HelseID: `/confirm-client/<client_id>?redirectPort=<port>&redirectPath=<path>`, where:

- `<client_id>` is the id of the client draft
- `<port>` and `<path>` is the port and path to redirect the end user back to your local http server

3. Check the status of the client's access to the specified scopes
4. Authenticate the end user and request access tokens for the specified apis
5. You're ready to go

## Updating the client

Look at the sample code in [ClientUpdateExample](https://github.com/NorskHelsenett/Selvbetjening.Samples/tree/main/ClientUpdateExample)

There are two separate endpoints:

1. Updating the client secret
2. Updating the rest of the client configuration

The update operation will affect all properties in the payload as submitted. If a property is set to null in the payload, the property **will not** be ignored and will be updated in the client configuration.

### Example for redirect uris:

If you want to add a redirect uri, you also need to submit the previous redirect uris, along with the rest of the relevant data for the client configuration.  
If you want to delete a redirect uri, just remove it from the array and call the update endpoint with the rest of the data.  
If you set redirect uris to be null, the update will fail if the client system isn't configured to use the same redirect uris for all client configurations.
