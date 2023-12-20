
namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnColorPicker : ColumnBaseType
{
    public string? Color { get; set; }

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
