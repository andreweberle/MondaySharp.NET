using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.JsonConverters;

internal class StringToMondayStateEnumJsonConverter : JsonConverter<MondayState?>
{
    public override MondayState? ReadJson(JsonReader reader, Type objectType, MondayState? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        // Try to parse the value to a ulong.
        if (Enum.TryParse(reader.Value?.ToString(), out MondayState result))
        {
            // Return the ulong.
            return result;
        }

        // Return null.
        return default;
    }

    public override void WriteJson(JsonWriter writer, MondayState? value, JsonSerializer serializer)
    {
        // Write the value.
        writer.WriteValue(value);
    }
}