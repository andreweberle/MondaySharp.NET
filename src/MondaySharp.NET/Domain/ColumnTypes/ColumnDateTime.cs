namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnDateTime : ColumnBaseType
{
    public ColumnDateTime() { }
    public DateTime? Date { get; set; }
    public bool IncludeTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dateTime"></param>
    public ColumnDateTime(string? id, DateTime? dateTime, bool includeTime = false)
    {
        this.Id = id;
        this.Date = dateTime;
        this.IncludeTime = includeTime;
    }
    public ColumnDateTime(string? id, string dateTime, string includeTime = "false")
    {
        this.Id = id;
        this.Date = DateTime.Parse(dateTime);
        this.IncludeTime = Convert.ToBoolean(includeTime);
    }

    public ColumnDateTime(string? id, string dateTime)
    {
        this.Id = id;
        this.Date = DateTime.Parse(dateTime);
        this.IncludeTime = this.Date?.TimeOfDay.TotalSeconds != 0;
    }

    public override string ToString()
    {
        return this.IncludeTime
            ? "\"" + this.Id + "\" : {\"date\" : \"" + this.Date?.ToString("yyyy-MM-dd") + "\", \"time\" : \"" + this.Date?.ToString("HH:mm:ss") + "\"}"
            : "\"" + this.Id + "\" : {\"date\" : \"" + this.Date?.ToString("yyyy-MM-dd") + "\"}";
    }
}
