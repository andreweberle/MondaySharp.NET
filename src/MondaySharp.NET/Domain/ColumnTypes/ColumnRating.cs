using MondaySharp.NET.Domain.Common.Enums;

namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnRating : ColumnBaseType
{
    public MondayRating? Rating { get; set; }

    public ColumnRating()
    {
    }

    public ColumnRating(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rating"></param>
    public ColumnRating(string? id, MondayRating? rating)
    {
        Id = id;
        Rating = rating;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"\"{Id}\" : {{\"rating\" : {(int?)Rating ?? 0}}}";
    }
}