using Common.Models;
using IdentityModel.Client;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Auth.Utils;

public class AuthHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JwkWithMetadata? _dPoPKey;

    public AuthHttpClient(JwkWithMetadata? dPoPKey = null)
    {
        _httpClient = new HttpClient();
        _dPoPKey = dPoPKey;
    }

    public async Task<TResponse> Get<TResponse>(string uri, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Get);
        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage<TResponse>(response);
    }

    public async Task<TResponse> Post<TRequest, TResponse>(string uri, TRequest body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Post);
        SetJsonContent(requestMessage, body);

        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage<TResponse>(response);
    }

    public async Task Put<TRequest>(string uri, TRequest body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Put);
        SetJsonContent(requestMessage, body);

        var response = await _httpClient.SendAsync(requestMessage);
        await EnsureSuccess(response);
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

    private void SetJsonContent<TRequest>(HttpRequestMessage requestMessage, TRequest body)
    {
        requestMessage.Content = JsonContent.Create(body, options: JsonSerializerOptions);
    }

    private static async Task<TResponse> GetResponseMessage<TResponse>(HttpResponseMessage response)
    {
        await EnsureSuccess(response);

        var responseBody = await response.Content.ReadFromJsonAsync<TResponse>(options: JsonSerializerOptions);
        if (responseBody == null)
        {
            throw new Exception("Response contained null value");
        }

        return responseBody;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var responseMessage = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http request failed. Code: {response.StatusCode} Content: {responseMessage}");
        }
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