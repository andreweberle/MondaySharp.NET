using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record Group
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("title")] public string? Title { get; set; }

    [JsonProperty("color")] public string? Color { get; set; }

    [JsonProperty("archived")] public bool Archived { get; set; }

    [JsonProperty("deleted")] public bool Deleted { get; set; }

    [JsonProperty("position")] public string? Position { get; set; }

    [JsonProperty("items_page")] public List<Item>? Items { get; set; }
}