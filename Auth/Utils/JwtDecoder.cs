using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Auth.Utils;

public static class JwtDecoder
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions;

    public static string Decode(string jwt)
    {
        string payload = jwt.Split(".")[1].Replace('_', '/').Replace('-', '+');

        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        string json = Encoding.Default.GetString(Convert.FromBase64String(payload));

        var jsonObject = JsonSerializer.Deserialize<JsonObject>(json, options: _jsonSerializerOptions);
        string formattedJson = JsonSerializer.Serialize(jsonObject, options: _jsonSerializerOptions);

        return formattedJson;
    }

    static JwtDecoder()
    {
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true,
        };

        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }
}