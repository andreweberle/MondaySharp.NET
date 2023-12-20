using MondaySharp.NET.Application.JsonConverters;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record Update
{
    [JsonProperty("id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong? Id { get; set; }

    [JsonProperty("text_body")]
    public string? TextBody { get; set; }
}
