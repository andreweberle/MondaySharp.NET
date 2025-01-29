using MondaySharp.NET.Application.JsonConverters;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record Asset
{
    [JsonProperty("id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong? Id { get; set; }

    public ulong? ItemId { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("public_url")] public Uri? PublicUrl { get; set; }

    [JsonProperty("url_thumbnail")] public Uri? ThumbnailUrl { get; set; }

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("file_extension")] public string? FileExtension { get; set; }

    [JsonProperty("file_size")] public ulong? FileSize { get; set; }

    [JsonProperty("original_geometry")] public string? OriginalGeometry { get; set; }

    [JsonProperty("uploaded_by")] public User? UploadedBy { get; set; }

    [JsonProperty("url")] public Uri? Url { get; set; }
}