namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnLongText : ColumnBaseType
{
    public ColumnLongText()
    {
    }

    public string? Text { get; set; }

    public ColumnLongText(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="text"></param>
    public ColumnLongText(string? id, string? text)
    {
        Id = id;
        Text = text;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return "\"" + Id + "\" : null";
        }

        return "\"" + Id + "\" : {\"text\" : \"" + Text + "\"}";
    }
}