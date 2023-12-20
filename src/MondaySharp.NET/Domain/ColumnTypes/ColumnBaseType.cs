using Newtonsoft.Json;
using System.Text.Json;

namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnBaseType
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    public ColumnBaseType(string id) => this.Id = id;

    public ColumnBaseType()
    {
    }
}
