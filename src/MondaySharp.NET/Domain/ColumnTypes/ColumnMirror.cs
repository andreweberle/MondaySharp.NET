namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnMirror<T> : ColumnBaseType where T : ColumnBaseType
{
    public List<T> Items { get; set; } = [];
    public ColumnMirror(string? id, List<T> values)
    {
        Id = id;
        Items = values;
    }
    public ColumnMirror() {}
}