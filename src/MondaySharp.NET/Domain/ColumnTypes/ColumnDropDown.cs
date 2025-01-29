namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnDropDown : ColumnBaseType
{
    public ColumnDropDown()
    {
    }

    public string? Label { get; set; }
    public int? LabelId { get; set; }
    public string[]? Labels { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="labelId"></param>
    public ColumnDropDown(string? id, int? labelId)
    {
        Id = id;
        LabelId = labelId;
        Labels = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="label"></param>
    public ColumnDropDown(string? id, string? label)
    {
        Id = id;
        Label = label;
        Labels = Label?
            .Split(',', StringSplitOptions.TrimEntries
                        | StringSplitOptions.RemoveEmptyEntries) ?? [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="labels">Labels are case sensitive</param>
    public ColumnDropDown(string? id, string[] labels)
    {
        Id = id;
        Labels = labels;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    public ColumnDropDown(string? id)
    {
        Id = id;
        Labels = [];
    }

    public override string ToString()
    {
        if (Label != null)
        {
            return $"\"{Id}\" : {{\"labels\":[\"{Label}\"]}}";
        }

        if (Labels != null && Labels.Length > 0)
        {
            return $"\"{Id}\" : {{\"labels\":[{string.Join(",", Labels.Select(label => $"\"{label}\""))}]}}";
        }

        if (LabelId != null)
        {
            return $"\"{Id}\" : \"{LabelId}\"";
        }
        else
        {
            return $"\"{Id}\" : null";
        }
    }
}