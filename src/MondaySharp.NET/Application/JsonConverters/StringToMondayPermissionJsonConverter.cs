using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.JsonConverters;

internal class StringToMondayPermissionJsonConverter : JsonConverter<MondayPermission?>
{
    public override MondayPermission? ReadJson(JsonReader reader, Type objectType, MondayPermission? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (Enum.TryParse(reader.Value?.ToString(), out MondayPermission result))
        {
            return result;
        }

        return default;
    }

    public override void WriteJson(JsonWriter writer, MondayPermission? value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}