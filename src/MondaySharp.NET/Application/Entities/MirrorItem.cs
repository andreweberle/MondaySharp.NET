using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace MondaySharp.NET.Application.Entities;

public class MirrorItem
{
    [JsonProperty("mirrored_value")]
    public ColumnValue? MirroredValue { get; set; }
}
