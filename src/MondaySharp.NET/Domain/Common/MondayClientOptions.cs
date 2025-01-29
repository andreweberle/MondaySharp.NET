namespace MondaySharp.NET.Domain.Common;

public record MondayOptions
{
    public string? Version { get; set; } = "2023-10";
    public Uri? EndPoint { get; set; }
    public string? Token { get; set; }
}