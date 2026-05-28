namespace MondaySharp.NET.Application.Interfaces;

public interface IMondayIdentity
{
    public ulong Id { get; set; }
    public string? Name { get; set; }
}