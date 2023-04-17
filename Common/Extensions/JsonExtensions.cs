using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Extensions;

public static class JsonExtensions
{
    public static string ToJson<T>(this T t, bool indented = false)
    {
        var options = CreateOptions();

        options.WriteIndented = indented;

        return JsonSerializer.Serialize(t, options);
    }

    public static T? FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, CreateOptions());
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
}