using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Interfaces;

namespace MondaySharp.NET.Domain.Common;

public record MondaySubRowLite;
public record MondaySubRow : IMondayIdentity
{
    [MondayColumnHeader("id")] public ulong Id { get; set; }

    [MondayColumnHeader("name")] public string? Name { get; set; }
}