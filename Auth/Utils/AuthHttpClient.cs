using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Auth.Utils;

public class AuthHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public AuthHttpClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task<T> Get<T>(string uri, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string json = await Get(uri, accessToken, headers);

        return Deserialize<T>(json);
    }

    public async Task<ResponseType> Post<RequestType, ResponseType>(string uri, RequestType body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        string jsonBody = JsonSerializer.Serialize(body, _jsonSerializerOptions);
        string jsonResponse = await Post(uri, jsonBody, accessToken, headers);

        return Deserialize<ResponseType>(jsonResponse);
    }

    public async Task<string> Get(string uri, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        using var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Get);

        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage(response);
    }

    public async Task<string> Post(string uri, string body, string? accessToken = null, IDictionary<string, string>? headers = null)
    {
        using var requestMessage = CreateRequestMessage(uri, accessToken, headers, HttpMethod.Post);

        requestMessage.Content = new StringContent(body, MediaTypeHeaderValue.Parse("application/json"));

        var response = await _httpClient.SendAsync(requestMessage);

        return await GetResponseMessage(response);
    }

    private static HttpRequestMessage CreateRequestMessage(string uri, string? accessToken, IDictionary<string, string>? headers, HttpMethod method)
    {
        var requestMessage = new HttpRequestMessage(method, uri);

        if (accessToken != null)
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
        var deserialized = JsonSerializer.Deserialize<T>(json, options: _jsonSerializerOptions);

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