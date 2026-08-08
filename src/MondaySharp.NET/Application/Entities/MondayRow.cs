namespace MondaySharp.NET.Application.Entities;

public record MondayRow
{
    public ulong Id { get; set; }
    public string? Name { get; set; }
    public Board? Board { get; set; }
}
