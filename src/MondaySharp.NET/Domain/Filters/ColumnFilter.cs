namespace MondaySharp.NET.Domain.Filters;

public record ColumnFilter
{
    /// <summary>
    /// The whole text value to filter by "" (blank values)
    /// any_of
    /// </summary>
    public string? AnyOf { get; set; }

    /// <summary>
    /// The whole text value to filter by "" (blank values)
    /// not_any_of
    /// </summary>
    public string? NotAnyOf { get; set; }
}