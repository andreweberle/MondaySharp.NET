namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnText : ColumnBaseType
{
    /// <summary>
    /// 
    /// </summary>
    public ColumnText()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="text"></param>
    public ColumnText(string? id, string? text)
    {
        Id = id;
        Text = text?.Replace("\"", "'");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return "\"" + Id + "\" : null";
        }

        return "\"" + Id + "\" : \"" + Text + "\"";
    }
}