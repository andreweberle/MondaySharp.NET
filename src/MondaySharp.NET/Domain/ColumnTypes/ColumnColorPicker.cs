
using MondaySharp.NET.Application.Attributes;

namespace MondaySharp.NET.Domain.ColumnTypes;

[MondayColumnTypeUnsupportedWrite]
public record ColumnColorPicker : ColumnBaseType
{
    public string? Color { get; set; }

    public ColumnColorPicker(string? id)
    {
        this.Id = id;
    }

    public ColumnColorPicker(string? id, string? text)
    {
        this.Id = id;
        this.Color = text;
    }

    public ColumnColorPicker() { }

    public override string ToString()
    {
        throw new NotSupportedException(nameof(ColumnColorPicker));
    }
}
