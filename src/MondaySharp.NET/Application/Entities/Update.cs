﻿using MondaySharp.NET.Application.JsonConverters;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Entities;

public record Update
{
    [JsonProperty("id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong? Id { get; set; }

    public ulong? ItemId { get; set; }

    [JsonProperty("text_body")] public string? TextBody { get; set; }

    [JsonIgnore] public FileUpload? FileUpload { get; set; }
}

public record FileUpload
{
    public string? FileName { get; set; }
    public StreamContent? StreamContent { get; set; }
    public string? ColumnId { get; set; }
}