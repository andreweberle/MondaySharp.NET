using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MondaySharp.NET.Application.Interfaces;

public interface IMondayBindableSource
{
    public ulong Id { get; }
    public string? Name { get; }

    [JsonProperty("state")] [JsonConverter(typeof(StringEnumConverter))] public MondayState? State { get; }
    public Board? Board { get; }
    public Group? Group { get; }

    public IReadOnlyList<ColumnValue> ColumnValues { get; }
    public IReadOnlyList<SubItem> SubItems { get; }
    public IReadOnlyList<Asset> Assets { get; }
    public IReadOnlyList<Update> Updates { get; }

    public FileUpload? FileUpload { get; }
}