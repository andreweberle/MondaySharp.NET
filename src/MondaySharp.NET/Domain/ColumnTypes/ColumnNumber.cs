namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnNumber : ColumnBaseType
{
    public ColumnNumber() { }
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
        this.Id = id;
        this.Number = number;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() => "\"" + this.Id + "\" : \"" + this.Number + "\"";
}
