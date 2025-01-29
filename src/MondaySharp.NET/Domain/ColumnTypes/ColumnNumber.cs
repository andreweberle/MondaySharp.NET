namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnNumber : ColumnBaseType
{
    public ColumnNumber()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public float? Number { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="number"></param>
    public ColumnNumber(string? id, float? number)
    {
        Id = id;
        Number = number;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (Number == null)
        {
            return "\"" + Id + "\" : null";
        }

        return "\"" + Id + "\" : \"" + Number + "\"";
    }
}