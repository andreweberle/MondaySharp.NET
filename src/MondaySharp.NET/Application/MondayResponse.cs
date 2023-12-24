namespace MondaySharp.NET.Application;

public record MondayResponse<T>
{
    public bool IsSuccessful { get; set; }
    public HashSet<string>? Errors { get; set; }
    public T? Data { get; set; }
}
