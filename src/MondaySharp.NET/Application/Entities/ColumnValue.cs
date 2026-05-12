using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using System.Text.Json;

namespace MondaySharp.NET.Application.Entities;

public record ColumnValue
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("column")] public Column? Column { get; set; }

    [JsonProperty("text")] public string? Text { get; set; }

    [JsonProperty("type")] public MondayColumnType Type { get; set; }

    [JsonProperty("__typename")] public string? TypeName { get; set; }

    [JsonProperty("persons_and_teams")] public List<PeopleEntity> PeopleAndTeams { get; set; } = [];

    [JsonProperty("mirrored_items")] public List<MirrorItem> MirroredItems { get; set; } = [];

    [JsonIgnore] public ColumnBaseType? ColumnBaseType { get; set; }
}