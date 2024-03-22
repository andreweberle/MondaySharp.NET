using MondaySharp.NET.Domain.Common.Enums;

namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnRating : ColumnBaseType
{
    public MondayRating? Rating { get; set; }

    public ColumnRating() { }

    public ColumnRating(string? id)
    {
        this.Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rating"></param>
    public ColumnRating(string? id, MondayRating? rating)
    {
        this.Id = id;
        this.Rating = rating;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"\"{this.Id}\" : {{\"rating\" : {(int?)this.Rating ?? 0}}}";
    }
}
