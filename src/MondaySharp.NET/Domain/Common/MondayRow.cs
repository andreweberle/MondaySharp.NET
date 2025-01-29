using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Domain.ColumnTypes;

namespace MondaySharp.NET.Domain.Common;

public record MondayRow
{
    [MondayColumnHeader("id")] public ulong Id { get; set; }

    [MondayColumnHeader("name")] public string? Name { get; set; }
}