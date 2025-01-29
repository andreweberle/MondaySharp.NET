namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnCheckBox : ColumnBaseType
{
    public bool IsChecked { get; set; }

    public ColumnCheckBox()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isChecked"></param>
    public ColumnCheckBox(string? id, bool isChecked)
    {
        Id = id;
        IsChecked = isChecked;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return IsChecked ? $"\"check\" : {{\"checked\" : \"{IsChecked.ToString().ToLower()}\"}}" : $"\"{Id}\" : null";
    }
}