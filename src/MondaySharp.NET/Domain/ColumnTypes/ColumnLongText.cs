namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnLongText : ColumnBaseType
{
    public ColumnLongText() { }
    public string? Text { get; set; }

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
    public override string ToString() => "\"" + this.Id + "\" : {\"text\" : \"" + this.Text + "\"}";
}
