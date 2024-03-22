namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnTag : ColumnBaseType
{
    public ColumnTag() { }
    public int[]? TagIds { get; set; }
    public string[]? Tags { get; set; }

    public ColumnTag(string? id, string? text)
    {
        this.Id = id;

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Split at , and try parse into int array if possible.
            this.TagIds = text
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int result) ? result : (int?)null)
                .Where(i => i.HasValue)
                .Select(i => i!.Value)
                .ToArray();

            if (this.TagIds.Length == 0)
            {
                this.Tags = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }

    public ColumnTag(string id, int[] tags)
    {
        this.Id = id;
        this.TagIds = tags;
    }

    public override string ToString()
    {
        return TagIds?.Length switch
        {
            > 0 => "\"" + Id + "\" : {\"tag_ids\" : [" + string.Join(",", TagIds) + "]}",
            _ => "\"" + Id + "\" : {\"tag_ids\" : []}"
        };
    }
}
