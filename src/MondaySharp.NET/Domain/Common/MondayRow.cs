using MondaySharp.NET.Domain.ColumnTypes;

namespace MondaySharp.NET.Domain.Common;

public record MondayRow
{
    public ulong Id { get; set; }
    public ColumnText? Name { get; set; }
}
