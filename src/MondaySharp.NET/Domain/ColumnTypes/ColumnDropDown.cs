namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnDropDown : ColumnBaseType
{
    public ColumnDropDown() { }
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
        this.Id = id;
        this.LabelId = labelId;
        this.Labels = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="label"></param>
    public ColumnDropDown(string? id, string? label)
    {
        this.Id = id;
        this.Label = label;
        this.Labels = this.Label?
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
        this.Id = id;
        this.Labels = labels;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    public ColumnDropDown(string? id)
    {
        this.Id = id;
        this.Labels = [];
    }

    public override string ToString()
    {
        if (this.Label != null)
        {
            return $"\"{this.Id}\" : {{\"labels\":[\"{this.Label}\"]}}";
        }

        if (this.Labels != null && this.Labels.Length > 0)
        {
            return $"\"{this.Id}\" : {{\"labels\":[{string.Join(",", this.Labels.Select(label => $"\"{label}\""))}]}}";
        }

        if (this.LabelId != null)
        {
            return $"\"{this.Id}\" : \"{this.LabelId}\"";
        }
        else
        {
            return $"\"{this.Id}\" : null";
        }
    }
}
