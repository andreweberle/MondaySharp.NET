using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record SubItem
{
    [JsonProperty("id")] public ulong Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("column_values")] public List<ColumnValue> ColumnValues { get; set; } = [];
}