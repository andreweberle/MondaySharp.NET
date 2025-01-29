using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MondaySharp.NET.Application.JsonConverters;

internal class StringToJsonDocumentJsonConverter : JsonConverter<JsonDocument?>
{
    public override JsonDocument? ReadJson(JsonReader reader, Type objectType, JsonDocument? existingValue,
        bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        // If the value is null or empty, return null.
        if (reader.TokenType == JsonToken.Null ||
            (reader.TokenType == JsonToken.String && string.IsNullOrEmpty(reader.Value?.ToString()))) return default;

        // Create Utf8JsonReader.
        Utf8JsonReader utf8JsonReader = new(Encoding.UTF8.GetBytes(reader.Value.ToString()!));

        if (JsonDocument.TryParseValue(ref utf8JsonReader, out JsonDocument? result))
        {
            return result;
        }
        else
        {
            return default;
        }
    }

    public override void WriteJson(JsonWriter writer, JsonDocument? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        // Write the value.
        writer.WriteValue(value);
    }
}