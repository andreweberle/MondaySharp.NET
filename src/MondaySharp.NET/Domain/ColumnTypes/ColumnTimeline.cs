namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnTimeline : ColumnBaseType
{
    public ColumnTimeline() { }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public ColumnTimeline(string? id, DateTime from, DateTime to)
    {
        this.Id = id;
        this.From = from;
        this.To = to;
    }

    public ColumnTimeline(string? id, string from, string to)
    {
        this.Id = id;
        this.From = DateTime.Parse(from);
        this.To = DateTime.Parse(to);
    }

    public ColumnTimeline(string? id)
    {
        this.Id = id;
    }

    public override string ToString()
    {
        if (this.From == null && this.To == null)
        {
            return "\"" + this.Id + "\" : null";
        }

        return this.From != null && this.To != null
            ? "\"" + this.Id + "\" : {\"from\" : \"" + this.From.Value.ToString("yyyy-MM-dd") + "\", \"to\" : \"" + this.To.Value.ToString("yyyy-MM-dd") + "\"}"
            : base.ToString();
    }
}
