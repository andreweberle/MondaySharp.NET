using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.JsonConverters;

internal class StringToMondayBoardKindJsonConverter : JsonConverter<MondayBoardKind?>
{
    public override MondayBoardKind? ReadJson(JsonReader reader, Type objectType, MondayBoardKind? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (Enum.TryParse(reader.Value?.ToString(), out MondayBoardKind result))
        {
            return result;
        }

        return default;
    }

    public override void WriteJson(JsonWriter writer, MondayBoardKind? value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}
