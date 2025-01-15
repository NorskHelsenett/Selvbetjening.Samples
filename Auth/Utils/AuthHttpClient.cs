using Common.Models;
using IdentityModel.Client;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Auth.Utils;

public class AuthHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JwkWithMetadata? _dPoPKey;

    public AuthHttpClient(JwkWithMetadata? dPoPKey = null, bool logHttp = false)
    {
        _httpClient = logHttp
            ? new HttpClient(new RawHttpLoggingHandler(new HttpClientHandler()))
            : new HttpClient();
        _dPoPKey = dPoPKey;
    }

    public async Task<T> Get<T>(string uri, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string json = await Get(uri, accessToken, headers);

        return Deserialize<T>(json);
    }

    public async Task<TResponseType> Post<TRequestType, TResponseType>(string uri, TRequestType body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string jsonBody = body is string str ? str : JsonSerializer.Serialize(body, JsonSerializerOptions);
        string jsonResponse = await Post(uri, jsonBody, accessToken, headers);

        return Deserialize<TResponseType>(jsonResponse);
    }

    public async Task<TResponseType> Put<TRequestType, TResponseType>(string uri, TRequestType body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string jsonBody = JsonSerializer.Serialize(body, JsonSerializerOptions);
        string jsonResponse = await Put(uri, jsonBody, accessToken, headers);

        return Deserialize<TResponseType>(jsonResponse);
    }

    public async Task Put<TRequestType>(string uri, TRequestType body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string jsonBody = JsonSerializer.Serialize(body, JsonSerializerOptions);
        var _ = await Put(uri, jsonBody, accessToken, headers);
    }

    public async Task<string> Get(string uri, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        using var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Get);

        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage(response);
    }

    public async Task<string> Post(string uri, string body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        return await SendWithContent(uri, body, accessToken, headers, HttpMethod.Post);
    }

    public async Task<string> Put(string uri, string body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        return await SendWithContent(uri, body, accessToken, headers, HttpMethod.Put);
    }

    private async Task<string> SendWithContent(string uri, string body, string? accessToken, IDictionary<string, string>? headers, HttpMethod method)
    {
        var requestMessage = CreateRequestMessage(uri, accessToken, headers, method);

        requestMessage.Content = new StringContent(body, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage(response);
    }

    private HttpRequestMessage CreateRequestMessage(string uri, string? accessToken, IDictionary<string, string>? headers, HttpMethod method)
    {
        var requestMessage = new HttpRequestMessage(method, uri);

        if (_dPoPKey != null && accessToken != null)
        {
            var dPopProof = DPoPProofBuilder.CreateDPoPProof(uri, method.ToString(), _dPoPKey, accessToken: accessToken);
            requestMessage.SetDPoPToken(accessToken, dPopProof);
        }
        else if (accessToken != null)
        {
            requestMessage.SetBearerToken(accessToken);
        }

        requestMessage.Headers.Add("Accept", "application/json");

        if (headers != null)
        {
            foreach (var kvp in headers)
            {
                requestMessage.Headers.Add(kvp.Key, kvp.Value);
            }
        }

        return requestMessage;
    }

    private static async Task<string> GetResponseMessage(HttpResponseMessage response)
    {
        string responseMessage = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Http request failed. Code: {response.StatusCode} Content: {responseMessage}");
        }

        return responseMessage;
    }

    private static T Deserialize<T>(string json)
    {
        var deserialized = JsonSerializer.Deserialize<T>(json, options: JsonSerializerOptions);

        if (deserialized == null)
        {
            throw new Exception($"{json} was deserialized as null.");
        }

        return deserialized;
    }

    public void Dispose()
    {
        using (_httpClient) { }
    }

    static AuthHttpClient()
    {
        JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true,
        };

        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions;
}