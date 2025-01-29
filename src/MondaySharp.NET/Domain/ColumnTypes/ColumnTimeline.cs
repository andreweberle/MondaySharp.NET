namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnTimeline : ColumnBaseType
{
    public ColumnTimeline()
    {
    }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public ColumnTimeline(string? id, DateTime from, DateTime to)
    {
        Id = id;
        From = from;
        To = to;
    }

    public ColumnTimeline(string? id, string from, string to)
    {
        Id = id;
        From = DateTime.Parse(from);
        To = DateTime.Parse(to);
    }

    public ColumnTimeline(string? id)
    {
        Id = id;
    }

    public override string ToString()
    {
        if (From == null && To == null)
        {
            return "\"" + Id + "\" : null";
        }

        return From != null && To != null
            ? "\"" + Id + "\" : {\"from\" : \"" + From.Value.ToString("yyyy-MM-dd") + "\", \"to\" : \"" +
              To.Value.ToString("yyyy-MM-dd") + "\"}"
            : base.ToString();
    }
}