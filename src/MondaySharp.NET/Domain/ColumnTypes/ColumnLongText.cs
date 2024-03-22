namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnLongText : ColumnBaseType
{
    public ColumnLongText() { }
    public string? Text { get; set; }

    public ColumnLongText(string? id)
    {
        this.Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="text"></param>
    public ColumnLongText(string? id, string? text)
    {
        this.Id = id;
        this.Text = text;
    }
    public override string ToString()
    {
        if (string.IsNullOrEmpty(this.Text))
        {
            return "\"" + this.Id + "\" : null";
        }

        return "\"" + this.Id + "\" : {\"text\" : \"" + this.Text + "\"}";
    }
}
