using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MondaySharp.NET.Application.Entities;

public record PeopleEntity
{
    [JsonProperty("id")] public string? Id { get; set; }
    [JsonProperty("kind")] [JsonConverter(typeof(StringEnumConverter))] public MondayPeopleEntityType Kind { get; set; }
}