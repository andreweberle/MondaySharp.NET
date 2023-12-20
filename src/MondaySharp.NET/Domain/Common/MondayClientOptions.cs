namespace MondaySharp.NET.Domain.Common;

public record MondayOptions
{
    public string? Version { get; set; }
    public Uri? EndPoint { get; set; }
    public string? Token { get; set; }
}
