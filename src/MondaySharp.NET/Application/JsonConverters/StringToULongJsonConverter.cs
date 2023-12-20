using Newtonsoft.Json;

namespace MondaySharp.NET.Application.JsonConverters;

internal class StringToULongJsonConverter : JsonConverter<ulong?>
{
    public override ulong? ReadJson(JsonReader reader, Type objectType,
        ulong? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Try to parse the value to a ulong.
        if (ulong.TryParse(reader.Value?.ToString(), out ulong result))
        {
            // Return the ulong.
            return result;
        }

        // Return null.
        return null;
    }

    public override void WriteJson(JsonWriter writer, ulong? value, JsonSerializer serializer)
    {
        // Write the value.
        writer.WriteValue(value);
    }
}
