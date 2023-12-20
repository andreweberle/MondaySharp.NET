using MondaySharp.NET.Application.JsonConverters;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record Asset
{
    [JsonProperty("id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong? Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("public_url")]
    public Uri? PublicUrl { get; set; }

    [JsonProperty("url_thumbnail")]
    public Uri? ThumbnailUrl { get; set;}

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
 }
