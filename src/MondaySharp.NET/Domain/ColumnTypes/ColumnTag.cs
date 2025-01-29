namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnTag : ColumnBaseType
{
    public ColumnTag()
    {
    }

    public int[]? TagIds { get; set; }
    public string[]? Tags { get; set; }

    public ColumnTag(string? id)
    {
        Id = id;
    }

    public ColumnTag(string? id, string? text)
    {
        Id = id;

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Split at , and try parse into int array if possible.
            TagIds = text
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int result) ? result : (int?)null)
                .Where(i => i.HasValue)
                .Select(i => i!.Value)
                .ToArray();

            if (TagIds.Length == 0)
            {
                Tags = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }

    public ColumnTag(string id, int[] tags)
    {
        Id = id;
        TagIds = tags;
    }

    public override string ToString()
    {
        return TagIds?.Length switch
        {
            > 0 => "\"" + Id + "\" : {\"tag_ids\" : [" + string.Join(",", TagIds) + "]}",
            _ => "\"" + Id + "\" : null"
        };
    }
}