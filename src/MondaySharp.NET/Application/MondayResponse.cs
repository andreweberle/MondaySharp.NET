namespace MondaySharp.NET.Application;

public record MondayResponse<T>
{
    public bool IsSuccessful { get; set; }
    public HashSet<string>? Errors { get; set; }
    public string? Cursor { get; set; }
    public bool HasMore => !string.IsNullOrEmpty(Cursor);
    public List<MondayData<T>>? Response { get; set; }
}

public record MondayData<T>
{
    public T? Data { get; set; }
}
