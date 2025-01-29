namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnDateTime : ColumnBaseType
{
    public ColumnDateTime()
    {
    }

    public DateTime? Date { get; set; }
    public bool IncludeTime { get; set; }

    public ColumnDateTime(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dateTime"></param>
    public ColumnDateTime(string? id, DateTime? dateTime, bool includeTime = false)
    {
        Id = id;
        Date = dateTime;
        IncludeTime = includeTime;
    }

    public ColumnDateTime(string? id, string dateTime, string includeTime = "false")
    {
        Id = id;
        Date = DateTime.Parse(dateTime);
        IncludeTime = Convert.ToBoolean(includeTime);
    }

    public ColumnDateTime(string? id, string dateTime)
    {
        Id = id;
        Date = DateTime.Parse(dateTime);
        IncludeTime = Date?.TimeOfDay.TotalSeconds != 0;
    }

    public override string ToString()
    {
        if (Date == null)
        {
            return "\"" + Id + "\" : null";
        }

        return IncludeTime
            ? "\"" + Id + "\" : {\"date\" : \"" + Date?.ToString("yyyy-MM-dd") + "\", \"time\" : \"" +
              Date?.ToString("HH:mm:ss") + "\"}"
            : "\"" + Id + "\" : {\"date\" : \"" + Date?.ToString("yyyy-MM-dd") + "\"}";
    }
}