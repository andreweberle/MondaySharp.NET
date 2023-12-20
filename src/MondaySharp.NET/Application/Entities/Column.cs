using MondaySharp.NET.Application.JsonConverters;
using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MondaySharp.NET.Application.Entities;

public record Column
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("archived")]
    public bool Archived { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("settings_str")]
    public string? SettingsStr { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MondayColumnType? Type { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }
}
