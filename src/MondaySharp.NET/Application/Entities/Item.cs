using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MondaySharp.NET.Application.Entities;

public record Item
{
    [JsonProperty("id")] public ulong Id { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MondayState? State { get; set; }

    [JsonProperty("board")] public Board? Board { get; set; }

    [JsonProperty("group")] public Group? Group { get; set; }

    [JsonProperty("column_values")] public List<ColumnValue> ColumnValues { get; set; }

    [JsonProperty("assets")] public List<Asset> Assets { get; set; }

    [JsonProperty("updates")] public List<Update> Updates { get; set; }

    [JsonIgnore] public FileUpload? FileUpload { get; set; }

    public Item()
    {
        ColumnValues = [];
        Assets = [];
        Updates = [];
    }
}